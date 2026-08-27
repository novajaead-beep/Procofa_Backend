using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Procofa.Api.Bootstrap;
using Procofa.Api.Configuration;
using Procofa.Api.Endpoints;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.UseCases.Auth.Login;
using Procofa.Infrastructure;

// Instrucción 04, sección "BOOTSTRAP PRIMER ADMIN": host mode explícito,
// NUNCA un endpoint HTTP. Debe interceptarse ANTES de WebApplication.CreateBuilder
// para no levantar Kestrel/el pipeline HTTP completo por un comando de un solo
// disparo — ver BootstrapAdminRunner para el comando exacto.
if (args.Length > 0 && string.Equals(args[0], "bootstrap-admin", StringComparison.OrdinalIgnoreCase))
{
    return await BootstrapAdminRunner.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

// Instrucción 04.2 — CERRAR AUTH: ya NO se lee "ConnectionStrings:ProcofaDb"
// aquí. AddInfrastructure() registra ProcofaDbContext con el delegate
// (sp, options) => ... de AddDbContext, que resuelve IConfiguration desde el
// IServiceProvider recién en la primera resolución real del DbContext (por
// scope/request) — no en este punto, antes de builder.Build(). Ver
// Procofa.Infrastructure/DependencyInjection.cs para el detalle completo.
// Ausencia de "ConnectionStrings:ProcofaDb" NO impide arrancar: AddDbContext
// sigue siendo perezoso y "/health" no toca ProcofaDbContext (Instrucción 03:
// "/health debe funcionar sin conectarse a la BD").
builder.Services.AddSingleton(sp =>
    InfrastructureAuthSettingsFactory.Create(
        sp.GetRequiredService<IConfiguration>()));

builder.Services.AddInfrastructure();

// Instrucción 04: primer caso de uso real de Application — sin
// AddApplication(IServiceCollection) propio todavía (Application sigue sin
// depender de Microsoft.Extensions.DependencyInjection.Abstractions), se
// registra aquí directamente en el Composition Root.
builder.Services.AddScoped<LoginCommandHandler>();

var app = builder.Build();

// "Config validation al startup": los constructores de JwtAccessTokenGenerator
// y AuthPolicyOptionsAdapter validan Jwt:SigningKey/Issuer/Audience y los
// valores numéricos de Auth — pero como están registrados como singleton vía
// factory delegate, EF/DI no los invoca hasta la primera resolución real.
// Se fuerza esa resolución aquí, antes de aceptar tráfico, para fallar en
// el arranque (y no en el primer POST /api/auth/login de un usuario real).
app.Services.GetRequiredService<IAccessTokenGenerator>();
app.Services.GetRequiredService<IAuthPolicyOptions>();

// "ProblemDetails sin stack trace en respuesta": cualquier excepción no
// manejada por debajo de este punto responde con un ProblemDetails genérico
// — el detalle real solo va al log del servidor, nunca al cliente.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature is not null)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Procofa.Api.UnhandledException");
            logger.LogError(exceptionFeature.Error, "Excepción no manejada procesando {Path}", context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ha ocurrido un error interno.",
            Detail = "Ocurrió un error inesperado procesando la solicitud. Intente nuevamente más tarde.",
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponseAsync
});

app.MapAuthEndpoints();

app.Run();
return 0;

static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync($$"""{"status":"{{report.Status}}"}""");
}

// Expone el tipo de entry point para WebApplicationFactory<Program> en
// Procofa.Api.Tests (Instrucción 04) — los top-level statements generan
// "Program" internal por defecto; esta declaración lo hace visible desde
// el assembly de tests sin exponer nada más.
public partial class Program
{
}
