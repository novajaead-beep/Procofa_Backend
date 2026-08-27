using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.UseCases.Auth.BootstrapAdmin;

/// <summary>
/// Caso de uso one-shot "crear el primer ADMIN" (Instrucción 04, sección
/// "BOOTSTRAP PRIMER ADMIN"). Idempotente por diseño: si ya existe un
/// usuario con rol ADMIN en el tenant, no crea nada y devuelve
/// <see cref="BootstrapAdminOutcome.AlreadyExists"/> — nunca lanza ni
/// duplica. Nunca expuesto vía HTTP — invocado únicamente desde el host mode
/// de <c>Procofa.Api</c> (<c>dotnet run -- bootstrap-admin</c>).
/// </summary>
public sealed class BootstrapAdminCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPasswordHasher passwordHasher)
{
    public const string AdminRoleCode = "ADMIN";
    private const int MinimumPasswordLength = 8;

    public Task<BootstrapAdminResult> HandleAsync(BootstrapAdminCommand command, CancellationToken cancellationToken)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return Task.FromResult(BootstrapAdminResult.Failed(validationError));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<BootstrapAdminResult> ExecuteAsync(
        Guid tenantId, BootstrapAdminCommand command, CancellationToken ct)
    {
        // Idempotencia: "Solo debe funcionar si no existe ningún usuario ADMIN inicial".
        if (await userRepository.ExistsWithRoleAsync(tenantId, AdminRoleCode, ct))
        {
            return BootstrapAdminResult.AlreadyExists();
        }

        var adminRole = await roleRepository.FindByCodeAsync(AdminRoleCode, ct)
            ?? throw new InvalidOperationException(
                $"El rol '{AdminRoleCode}' no existe en el catálogo de roles — verifique que " +
                "db/baseline/v2.1/003_seed_catalogs.sql se haya ejecutado contra esta base de datos.");

        var passwordHash = passwordHasher.HashPassword(command.Password);

        var user = new User(
            Guid.NewGuid(),
            tenantId,
            command.Email,
            passwordHash,
            command.FirstName,
            command.LastName,
            phone: null);

        // assignedByUserId = null: no existe todavía ningún admin que asigne el rol (auto-bootstrap).
        user.AddRole(new UserRole(tenantId, user.Id, adminRole.Id, assignedByUserId: null));

        await userRepository.AddAsync(user, ct);

        return BootstrapAdminResult.Created(user.Id);
    }

    private static string? Validate(BootstrapAdminCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return "El email es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return "La contraseña es obligatoria.";
        }

        if (command.Password.Length < MinimumPasswordLength)
        {
            return $"La contraseña debe tener al menos {MinimumPasswordLength} caracteres.";
        }

        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            return "El nombre es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            return "El apellido es obligatorio.";
        }

        return null;
    }
}
