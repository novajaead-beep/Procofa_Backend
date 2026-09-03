using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.UseCases.Auth.RefreshSession;

public sealed class RefreshSessionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IAccessTokenGenerator accessTokenGenerator,
    IRefreshTokenFactory refreshTokenFactory,
    IAuthPolicyOptions authPolicyOptions,
    IDateTimeProvider dateTimeProvider)
{
    public Task<RefreshSessionResult> HandleAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.RawRefreshToken))
        {
            return Task.FromResult(
                RefreshSessionResult.Failure());
        }

        var tenantId = tenantContext.TenantId;
        var nowUtc = dateTimeProvider.UtcNow;

        return unitOfWork.ExecuteWriteAsync(
            ct => ExecuteAsync(
                tenantId,
                command.RawRefreshToken,
                nowUtc,
                ct),
            cancellationToken);
    }

    private async Task<RefreshSessionResult> ExecuteAsync(
        Guid tenantId,
        string rawRefreshToken,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var tokenHash =
            refreshTokenFactory.Hash(rawRefreshToken);

        var currentToken =
            await refreshTokenRepository.FindByHashForUpdateAsync(
                tenantId,
                tokenHash,
                cancellationToken);

        if (currentToken is null)
        {
            return RefreshSessionResult.Failure();
        }

        if (currentToken.IsRevoked)
        {
            await RevokeActiveTokensAsync(
                tenantId,
                currentToken.UserId,
                nowUtc,
                cancellationToken);

            return RefreshSessionResult.Failure();
        }

        if (currentToken.IsExpired(nowUtc))
        {
            currentToken.Revoke(nowUtc);

            return RefreshSessionResult.Failure();
        }

        var user = await userRepository.GetByIdAsync(
            tenantId,
            currentToken.UserId,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            await RevokeActiveTokensAsync(
                tenantId,
                currentToken.UserId,
                nowUtc,
                cancellationToken);

            return RefreshSessionResult.Failure();
        }

        var roleIds = user.Roles
            .Select(role => role.RoleId)
            .ToArray();

        var roleCodes =
            await userRepository.GetRoleCodesAsync(
                roleIds,
                cancellationToken);

        currentToken.Revoke(nowUtc);

        var generatedToken =
            refreshTokenFactory.Create();

        var newRefreshTokenExpiresAtUtc =
            nowUtc.Add(
                authPolicyOptions.RefreshTokenLifetime);

        var newRefreshToken = new RefreshToken(
            Guid.NewGuid(),
            tenantId,
            user.Id,
            generatedToken.TokenHash,
            newRefreshTokenExpiresAtUtc);

        await refreshTokenRepository.AddAsync(
            newRefreshToken,
            cancellationToken);

        var accessToken =
            accessTokenGenerator.GenerateAccessToken(
                user.Id,
                tenantId,
                user.Email,
                roleCodes);

        return RefreshSessionResult.Success(
            roleCodes,
            accessToken,
            generatedToken.RawToken,
            newRefreshTokenExpiresAtUtc);
    }

    private async Task RevokeActiveTokensAsync(
        Guid tenantId,
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var activeTokens =
            await refreshTokenRepository.ListActiveByUserAsync(
                tenantId,
                userId,
                nowUtc,
                cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(nowUtc);
        }
    }
}
