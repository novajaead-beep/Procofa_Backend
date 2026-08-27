using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Users;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.UseCases.Users.ReplaceUserClientAccess;

/// <summary>Caso de uso <c>PUT /api/users/{userId}/client-access</c> (Instrucción 05).</summary>
public sealed class ReplaceUserClientAccessCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IClientRepository clientRepository,
    ICurrentUser currentUser)
{
    public Task<ReplaceUserClientAccessResult> HandleAsync(
        ReplaceUserClientAccessCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var distinctClientIds = (command.ClientIds ?? []).Distinct().ToArray();

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var user = await userRepository.GetByIdAsync(tenantId, command.UserId, ct);
                if (user is null)
                {
                    return ReplaceUserClientAccessResult.Failure(ReplaceUserClientAccessError.NotFound);
                }

                var clienteRole = await roleRepository.FindByCodeAsync(UserRoleCodes.Cliente, ct);
                var hasClienteRole = clienteRole is not null && user.Roles.Any(r => r.RoleId == clienteRole.Id);

                if (!hasClienteRole)
                {
                    return ReplaceUserClientAccessResult.Failure(ReplaceUserClientAccessError.UserNotCliente);
                }

                if (distinctClientIds.Length > 0)
                {
                    var resolvedClients = await clientRepository.FindManyByIdsAsync(tenantId, distinctClientIds, ct);
                    if (resolvedClients.Count != distinctClientIds.Length)
                    {
                        return ReplaceUserClientAccessResult.Failure(ReplaceUserClientAccessError.ClientNotFound);
                    }
                }

                var newAccess = distinctClientIds
                    .Select(clientId => new UserClientAccess(tenantId, user.Id, clientId, grantedByUserId: currentUser.UserId))
                    .ToArray();

                user.ReplaceClientAccess(newAccess);

                return ReplaceUserClientAccessResult.Success();
            },
            cancellationToken);
    }
}
