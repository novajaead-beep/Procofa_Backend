using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Identity;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(
    ProcofaDbContext dbContext)
    : IRefreshTokenRepository
{
    public Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(refreshToken);

        return Task.CompletedTask;
    }

    public Task<RefreshToken?> FindByHashForUpdateAsync(
        Guid tenantId,
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM refresh_tokens
                WHERE tenant_id = {tenantId}
                  AND token_hash = {tokenHash}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(
        Guid tenantId,
        Guid userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        return await dbContext.RefreshTokens
            .Where(token =>
                token.TenantId == tenantId &&
                token.UserId == userId &&
                token.RevokedAtUtc == null &&
                token.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);
    }
}
