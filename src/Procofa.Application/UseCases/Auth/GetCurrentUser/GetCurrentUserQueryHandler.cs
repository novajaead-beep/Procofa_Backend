using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Auth.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository)
{
    public Task<GetCurrentUserResult> HandleAsync(
        CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var userId = currentUser.UserId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var user =
                    await userRepository.GetByIdAsync(
                        tenantId,
                        userId,
                        ct);

                if (user is null || !user.IsActive)
                {
                    return GetCurrentUserResult.NotFound();
                }

                var roleIds = user.Roles
                    .Select(role => role.RoleId)
                    .ToArray();

                var roles =
                    await userRepository.GetRoleCodesAsync(
                        roleIds,
                        ct);

                return GetCurrentUserResult.Success(
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.Phone,
                    user.MustChangePassword,
                    roles);
            },
            cancellationToken);
    }
}
