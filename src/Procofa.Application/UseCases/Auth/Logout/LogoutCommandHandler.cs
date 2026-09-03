using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Enums;

namespace Procofa.Application.UseCases.Auth.Logout;

public sealed class LogoutCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenFactory refreshTokenFactory,
    IAccessLogRepository accessLogRepository,
    IDateTimeProvider dateTimeProvider)
{
    public Task HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                command.RawRefreshToken))
        {
            return Task.CompletedTask;
        }

        var tenantId = tenantContext.TenantId;
        var nowUtc = dateTimeProvider.UtcNow;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var tokenHash =
                    refreshTokenFactory.Hash(
                        command.RawRefreshToken);

                var refreshToken =
                    await refreshTokenRepository
                        .FindByHashForUpdateAsync(
                            tenantId,
                            tokenHash,
                            ct);

                if (refreshToken is null ||
                    refreshToken.IsRevoked)
                {
                    return true;
                }

                refreshToken.Revoke(nowUtc);

                var accessLog = new AccessLog(
                    Guid.NewGuid(),
                    tenantId,
                    refreshToken.UserId,
                    attemptedEmail: null,
                    AccessLogEventType.Logout,
                    command.IpAddress,
                    command.UserAgent);

                await accessLogRepository.AddAsync(
                    accessLog,
                    ct);

                return true;
            },
            cancellationToken);
    }
}
