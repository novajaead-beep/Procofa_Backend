using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Procofa.Api.Configuration;
using Procofa.Application.UseCases.Auth.BootstrapAdmin;
using Procofa.Infrastructure;

namespace Procofa.Api.Bootstrap;

/// <summary>
/// Host mode explícito para el bootstrap one-shot del primer ADMIN
/// . Se invoca así, NUNCA vía HTTP: <code> PROCOFA_BOOTSTRAP_ADMIN_EMAIL=admin@procofa.com \
/// PROCOFA_BOOTSTRAP_ADMIN_PASSWORD='una-contraseña-segura-de-verdad' \
/// PROCOFA_BOOTSTRAP_ADMIN_FIRST_NAME=Admin \ PROCOFA_BOOTSTRAP_ADMIN_LAST_NAME=PROCOFA \ dotnet
/// run --project src/Procofa.Api -- bootstrap-admin </code> No hardcodea contraseña alguna, no la
/// persiste en texto plano (delega en <c>IPasswordHasher</c>), no requiere Jwt/AccessToken
/// configurado (el caso de uso no los usa), y es idempotente: una segunda ejecución detecta que ya
/// existe un ADMIN y termina con éxito sin duplicar (ver <see
/// cref="BootstrapAdminCommandHandler"/>). </summary>
internal static class BootstrapAdminRunner
{
    private const string EmailVariable = "PROCOFA_BOOTSTRAP_ADMIN_EMAIL";
    private const string PasswordVariable = "PROCOFA_BOOTSTRAP_ADMIN_PASSWORD";
    private const string FirstNameVariable = "PROCOFA_BOOTSTRAP_ADMIN_FIRST_NAME";
    private const string LastNameVariable = "PROCOFA_BOOTSTRAP_ADMIN_LAST_NAME";

    public static async Task<int> RunAsync()
    {
        var email = Environment.GetEnvironmentVariable(EmailVariable);
        var password = Environment.GetEnvironmentVariable(PasswordVariable);
        var firstName = Environment.GetEnvironmentVariable(FirstNameVariable);
        var lastName = Environment.GetEnvironmentVariable(LastNameVariable);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            // Falla de forma segura: nunca toca la BD, nunca revela cuál de
            // las variables faltó en más detalle del necesario (evita que un
            // log de CI exponga parcialmente una credencial mal seteada).
            await Console.Error.WriteLineAsync(
                $"bootstrap-admin: faltan variables de entorno requeridas ({EmailVariable}, " +
                $"{PasswordVariable}, {FirstNameVariable}, {LastNameVariable}). Ver el comando de " +
                "ejemplo en Procofa.Api/Bootstrap/BootstrapAdminRunner.cs.");
            return 1;
        }

        var hostBuilder = Host.CreateApplicationBuilder();

        // Ya no se captura la connection string aquí — AddInfrastructure() la resuelve de forma
        // diferida desde IConfiguration (registrada automáticamente por
        // Host.CreateApplicationBuilder en hostBuilder.Services) en el momento en que
        // ProcofaDbContext se construye, no antes. authSettings SÍ se registra como instancia ya
        // materializada aquí a propósito — este host mode nunca pasa por WebApplicationFactory, así
        // que no hay override de configuración tardío que perder; AddAuth() solo exige que
        // InfrastructureAuthSettings esté disponible en el contenedor cuando se resuelva.
        var authSettings = InfrastructureAuthSettingsFactory.Create(hostBuilder.Configuration);

        hostBuilder.Services.AddSingleton(authSettings);
        hostBuilder.Services.AddInfrastructure();
        hostBuilder.Services.AddScoped<BootstrapAdminCommandHandler>();

        using var host = hostBuilder.Build();
        using var scope = host.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<BootstrapAdminCommandHandler>();

        var command = new BootstrapAdminCommand(email, password, firstName, lastName);

        try
        {
            var result = await handler.HandleAsync(command, CancellationToken.None);

            switch (result.Outcome)
            {
                case BootstrapAdminOutcome.Created:
                    Console.WriteLine($"bootstrap-admin: usuario ADMIN creado (id={result.UserId}).");
                    return 0;

                case BootstrapAdminOutcome.AlreadyExists:
                    Console.WriteLine(
                        "bootstrap-admin: ya existe un usuario con rol ADMIN en el tenant PROCOFA — " +
                        "no se realizó ningún cambio (ejecución idempotente).");
                    return 0;

                case BootstrapAdminOutcome.ValidationFailed:
                    await Console.Error.WriteLineAsync($"bootstrap-admin: {result.ValidationError}");
                    return 1;

                default:
                    throw new InvalidOperationException(
                        $"{nameof(BootstrapAdminOutcome)} sin manejar: {result.Outcome}");
            }
        }
        catch (Exception ex)
        {
            // Mensaje de la excepción (config/conectividad de BD), nunca el
            // comando: password/hash jamás llegan a este catch por valor.
            await Console.Error.WriteLineAsync($"bootstrap-admin: error inesperado — {ex.Message}");
            return 2;
        }
    }
}
