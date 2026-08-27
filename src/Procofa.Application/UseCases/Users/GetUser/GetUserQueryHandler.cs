using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Users.GetUser;

/// <summary>Caso de uso <c>GET /api/users/{userId}</c> (Instrucción 05). Solo lectura.</summary>
public sealed class GetUserQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository)
{
    public Task<GetUserResult> HandleAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var user = await userRepository.GetByIdAsync(tenantId, query.UserId, ct);
                if (user is null)
                {
                    return GetUserResult.NotFound();
                }

                var roleIds = user.Roles.Select(r => r.RoleId).ToArray();
                var roleCodes = await userRepository.GetRoleCodesAsync(roleIds, ct);
                var clientAccess = user.ClientAccess
                    .Select(a => new UserClientAccessItem(a.ClientId))
                    .ToArray();

                return GetUserResult.Success(
                    user.Id, user.Email, user.FirstName, user.LastName, user.Phone, user.IsActive,
                    user.MustChangePassword, user.FailedLoginAttempts, user.LockedUntilUtc, user.LastLoginAtUtc,
                    user.CreatedAtUtc, user.UpdatedAtUtc, roleCodes, clientAccess);
            },
            cancellationToken);
    }
}
