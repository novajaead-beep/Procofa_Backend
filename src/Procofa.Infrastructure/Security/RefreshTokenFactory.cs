using System.Security.Cryptography;
using Procofa.Application.Abstractions.Identity;

namespace Procofa.Infrastructure.Security;

/// <summary>
/// Implementación de <see cref="IRefreshTokenFactory"/> (Instrucción 04,
/// sección "REFRESH TOKEN"): <see cref="RandomNumberGenerator"/> (nunca
/// <see cref="Random"/>) para el valor crudo, SHA-256 para el hash
/// persistido. El valor crudo se codifica Base64Url (sin relleno) para que
/// viaje limpio en JSON/headers sin caracteres que requieran escape.
/// </summary>
public sealed class RefreshTokenFactory : IRefreshTokenFactory
{
    private const int RawTokenSizeBytes = 32; // 256 bits de entropía.

    public GeneratedRefreshToken Create()
    {
        var rawBytes = RandomNumberGenerator.GetBytes(RawTokenSizeBytes);
        var rawToken = Base64UrlEncode(rawBytes);

        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken));
        var tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        return new GeneratedRefreshToken(rawToken, tokenHash);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
