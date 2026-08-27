using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Users;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.UseCases.Users.CreateUser;

/// <summary>
/// Caso de uso <c>POST /api/users</c> (Instrucción 05). Persiste
/// <c>User</c> + <c>UserRoles</c> + <c>UserClientAccess</c> en UNA sola
/// transacción tenant-scoped (<see cref="ITenantUnitOfWork.ExecuteWriteAsync{T}"/>).
/// </summary>
public sealed class CreateUserCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IClientRepository clientRepository,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser)
{
    public Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var validationError = Validate(command, out var roles, out var clientIds);
        if (validationError is not null)
        {
            return Task.FromResult(CreateUserResult.Failure(CreateUserError.ValidationFailed, validationError));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            ct => ExecuteAsync(tenantId, command!, roles!, clientIds!, ct), cancellationToken);
    }

    private async Task<CreateUserResult> ExecuteAsync(
        Guid tenantId,
        CreateUserCommand command,
        IReadOnlyCollection<string> roleCodes,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken ct)
    {
        var normalizedEmail = User.Normalize(command.Email!);

        if (await userRepository.ExistsByNormalizedEmailAsync(tenantId, normalizedEmail, ct))
        {
            return CreateUserResult.Failure(
                CreateUserError.EmailAlreadyExists, "Ya existe un usuario con ese email en el tenant actual.");
        }

        var resolvedRoles = await roleRepository.FindManyByCodesAsync(roleCodes, ct);
        if (resolvedRoles.Count != roleCodes.Count)
        {
            var missing = roleCodes.Except(resolvedRoles.Select(r => r.Code));
            return CreateUserResult.Failure(
                CreateUserError.RoleNotFound,
                $"Rol(es) no encontrados en el catálogo: {string.Join(", ", missing)}.");
        }

        if (clientIds.Count > 0)
        {
            var resolvedClients = await clientRepository.FindManyByIdsAsync(tenantId, clientIds, ct);
            if (resolvedClients.Count != clientIds.Count)
            {
                return CreateUserResult.Failure(
                    CreateUserError.ClientNotFound,
                    "Uno o más clientIds no existen o no pertenecen al tenant actual.");
            }
        }

        var passwordHash = passwordHasher.HashPassword(command.TemporaryPassword!);

        var user = new User(
            Guid.NewGuid(), tenantId, command.Email!, passwordHash,
            command.FirstName!, command.LastName!, command.Phone);

        // Sección "CREAR USUARIO": todo usuario creado por este endpoint debe
        // cambiar su contraseña temporal en el primer login.
        user.RequirePasswordChange();

        foreach (var role in resolvedRoles)
        {
            user.AddRole(new UserRole(tenantId, user.Id, role.Id, assignedByUserId: currentUser.UserId));
        }

        foreach (var clientId in clientIds)
        {
            user.GrantClientAccess(new UserClientAccess(tenantId, user.Id, clientId, grantedByUserId: currentUser.UserId));
        }

        await userRepository.AddAsync(user, ct);

        return CreateUserResult.Success(user.Id);
    }

    private static string? Validate(
        CreateUserCommand command, out IReadOnlyCollection<string>? roles, out IReadOnlyCollection<Guid>? clientIds)
    {
        roles = null;
        clientIds = null;

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return "El email es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            return "El nombre es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            return "El apellido es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.TemporaryPassword))
        {
            return "La contraseña temporal es obligatoria.";
        }

        var distinctRoles = (command.Roles ?? []).Distinct().ToArray();
        if (distinctRoles.Length == 0)
        {
            return "Debe asignarse al menos un rol.";
        }

        var invalidRoles = distinctRoles.Where(r => !UserRoleCodes.All.Contains(r)).ToArray();
        if (invalidRoles.Length > 0)
        {
            return $"Rol(es) no permitidos: {string.Join(", ", invalidRoles)}.";
        }

        var distinctClientIds = (command.ClientIds ?? []).Distinct().ToArray();
        var hasClienteRole = distinctRoles.Contains(UserRoleCodes.Cliente);

        if (!hasClienteRole && distinctClientIds.Length > 0)
        {
            return "clientIds solo puede asignarse a usuarios con rol CLIENTE.";
        }

        roles = distinctRoles;
        clientIds = distinctClientIds;
        return null;
    }
}
