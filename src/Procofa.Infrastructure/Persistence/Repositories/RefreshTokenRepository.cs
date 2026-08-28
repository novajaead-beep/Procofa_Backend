using Procofa.Application.Abstractions.Identity;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Repositories;

/// <summary>Implementación de <see cref="IRefreshTokenRepository"/> sobre <see
/// cref="ProcofaDbContext"/>.</summary>
public sealed class RefreshTokenRepository(ProcofaDbContext dbContext) : IRefreshTokenRepository
{
    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }
}
