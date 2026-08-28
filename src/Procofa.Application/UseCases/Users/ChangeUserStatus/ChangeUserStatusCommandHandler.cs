using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Users.ChangeUserStatus;

/// <summary>Caso de uso <c>PATCH /api/users/{userId}/status</c>. Nunca hace hard delete — solo
/// activa/desactiva.</summary>
public sealed class ChangeUserStatusCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    public Task<ChangeUserStatusResult> HandleAsync(ChangeUserStatusCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var user = await userRepository.GetByIdAsync(tenantId, command.UserId, ct);
                if (user is null)
                {
                    return ChangeUserStatusResult.Failure(ChangeUserStatusError.NotFound);
                }

                if (!command.IsActive && user.Id == currentUser.UserId)
                {
                    return ChangeUserStatusResult.Failure(ChangeUserStatusError.CannotDeactivateSelf);
                }

                if (command.IsActive)
                {
                    user.Activate();
                }
                else
                {
                    user.Deactivate();
                }

                return ChangeUserStatusResult.Success();
            },
            cancellationToken);
    }
}
