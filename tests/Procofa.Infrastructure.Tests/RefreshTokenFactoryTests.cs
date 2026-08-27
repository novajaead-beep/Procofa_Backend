using System.Security.Cryptography;
using System.Text;
using Procofa.Infrastructure.Security;

namespace Procofa.Infrastructure.Tests;

/// <summary>
/// Tests de <see cref="RefreshTokenFactory"/> (Instrucción 04, sección
/// "REFRESH TOKEN"): valor crudo único por llamada, hash SHA-256
/// determinista sobre ese valor, y (indirectamente, ver
/// <c>Procofa.IntegrationTests</c>) que solo el hash se persiste.
/// </summary>
public sealed class RefreshTokenFactoryTests
{
    [Fact]
    public void Create_ProduceUnRawTokenYUnHash_Diferentes()
    {
        var factory = new RefreshTokenFactory();

        var generated = factory.Create();

        Assert.False(string.IsNullOrWhiteSpace(generated.RawToken));
        Assert.False(string.IsNullOrWhiteSpace(generated.TokenHash));
        Assert.NotEqual(generated.RawToken, generated.TokenHash);
    }

    [Fact]
    public void Create_ElHash_EsSha256DelRawTokenEnHexadecimalMinusculas()
    {
        var factory = new RefreshTokenFactory();

        var generated = factory.Create();

        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(generated.RawToken)))
            .ToLowerInvariant();

        Assert.Equal(expectedHash, generated.TokenHash);
    }

    [Fact]
    public void Create_LlamadoDosVeces_ProduceValoresDistintos()
    {
        var factory = new RefreshTokenFactory();

        var first = factory.Create();
        var second = factory.Create();

        Assert.NotEqual(first.RawToken, second.RawToken);
        Assert.NotEqual(first.TokenHash, second.TokenHash);
    }
}
