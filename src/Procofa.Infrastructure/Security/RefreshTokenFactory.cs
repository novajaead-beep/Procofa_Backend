using System.Security.Cryptography;
using System.Text;
using Procofa.Application.Abstractions.Identity;

namespace Procofa.Infrastructure.Security;

public sealed class RefreshTokenFactory : IRefreshTokenFactory
{
    private const int RawTokenSizeBytes = 32;

    public GeneratedRefreshToken Create()
    {
        var rawBytes = RandomNumberGenerator.GetBytes(RawTokenSizeBytes);
        var rawToken = Base64UrlEncode(rawBytes);

        return new GeneratedRefreshToken(
            rawToken,
            Hash(rawToken));
    }

    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(rawToken));

        return Convert
            .ToHexString(hashBytes)
            .ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
