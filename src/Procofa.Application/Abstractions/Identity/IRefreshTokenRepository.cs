using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Abstractions.Identity;

public interface IRefreshTokenRepository
{
    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken);

    Task<RefreshToken?> FindByHashForUpdateAsync(
        Guid tenantId,
        string tokenHash,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(
        Guid tenantId,
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}
