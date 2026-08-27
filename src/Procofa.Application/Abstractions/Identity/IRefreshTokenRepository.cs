using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Abstractions.Identity;

/// <summary>Puerto de persistencia de <see cref="RefreshToken"/> (Instrucción 04: "Solo creación/persistencia durante login").</summary>
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);
}
