using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Users;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.UseCases.Users.ReplaceUserRoles;

/// <summary>
/// Caso de uso <c>PUT /api/users/{userId}/roles</c> (Instrucción 05).
/// Reemplaza el conjunto completo — nunca hace merge parcial. Si el nuevo
/// conjunto ya no incluye CLIENTE, limpia <c>user_client_access</c> (sección
/// "REGLAS PARA CLIENTE").
/// </summary>
public sealed class ReplaceUserRolesCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    ICurrentUser currentUser)
{
    public Task<ReplaceUserRolesResult> HandleAsync(
        ReplaceUserRolesCommand command, CancellationToken cancellationToken)
    {
        var distinctRoles = (command.Roles ?? []).Distinct().ToArray();

        if (distinctRoles.Length == 0)
        {
            return Task.FromResult(
                ReplaceUserRolesResult.Failure(
                    ReplaceUserRolesError.ValidationFailed, "Debe asignarse al menos un rol."));
        }

        var invalidRoles = distinctRoles.Where(r => !UserRoleCodes.All.Contains(r)).ToArray();
        if (invalidRoles.Length > 0)
        {
            return Task.FromResult(
                ReplaceUserRolesResult.Failure(
                    ReplaceUserRolesError.ValidationFailed,
                    $"Rol(es) no permitidos: {string.Join(", ", invalidRoles)}."));
        }

        // Sección "ASIGNAR ROLES": nunca permitir que un ADMIN se quite su
        // propio rol ADMIN — evaluado ANTES de tocar la BD (solo depende del
        // conjunto solicitado y de quién llama, ambos ya conocidos aquí).
        if (command.UserId == currentUser.UserId && !distinctRoles.Contains(UserRoleCodes.Admin))
        {
            return Task.FromResult(
                ReplaceUserRolesResult.Failure(ReplaceUserRolesError.CannotRemoveOwnAdminRole));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var user = await userRepository.GetByIdAsync(tenantId, command.UserId, ct);
                if (user is null)
                {
                    return ReplaceUserRolesResult.Failure(ReplaceUserRolesError.NotFound);
                }

                var resolvedRoles = await roleRepository.FindManyByCodesAsync(distinctRoles, ct);
                if (resolvedRoles.Count != distinctRoles.Length)
                {
                    var missing = distinctRoles.Except(resolvedRoles.Select(r => r.Code));
                    return ReplaceUserRolesResult.Failure(
                        ReplaceUserRolesError.RoleNotFound,
                        $"Rol(es) no encontrados en el catálogo: {string.Join(", ", missing)}.");
                }

                var newUserRoles = resolvedRoles
                    .Select(role => new UserRole(tenantId, user.Id, role.Id, assignedByUserId: currentUser.UserId))
                    .ToArray();

                user.ReplaceRoles(newUserRoles);

                if (!distinctRoles.Contains(UserRoleCodes.Cliente))
                {
                    user.ClearClientAccess();
                }

                return ReplaceUserRolesResult.Success();
            },
            cancellationToken);
    }
}
