using Procofa.Application.Abstractions.Identity;
using Procofa.Infrastructure.Security;

namespace Procofa.Infrastructure.Tests;

/// <summary>
/// Tests de <see cref="PasswordHasherAdapter"/>. No requiere BD — ejercita
/// <c>PasswordHasher&lt;TUser&gt;</c> de ASP.NET Core Identity a través del adapter real.
/// </summary>
public sealed class PasswordHasherAdapterTests
{
    [Fact]
    public void HashPassword_ProduceUnHash_DiferenteDeLaContraseñaEnTextoPlano()
    {
        var hasher = new PasswordHasherAdapter();

        var hash = hasher.HashPassword("una-contraseña-segura");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("una-contraseña-segura", hash);
    }

    [Fact]
    public void VerifyPassword_ConLaContraseñaCorrecta_DevuelveSuccess()
    {
        var hasher = new PasswordHasherAdapter();
        var hash = hasher.HashPassword("una-contraseña-segura");

        var result = hasher.VerifyPassword(hash, "una-contraseña-segura");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void VerifyPassword_ConLaContraseñaIncorrecta_DevuelveFailed()
    {
        var hasher = new PasswordHasherAdapter();
        var hash = hasher.HashPassword("una-contraseña-segura");

        var result = hasher.VerifyPassword(hash, "otra-contraseña-distinta");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void DosHashesDeLaMismaContraseña_SonDistintos_PorElSalt()
    {
        var hasher = new PasswordHasherAdapter();

        var hash1 = hasher.HashPassword("misma-contraseña");
        var hash2 = hasher.HashPassword("misma-contraseña");

        Assert.NotEqual(hash1, hash2);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyPassword(hash1, "misma-contraseña"));
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyPassword(hash2, "misma-contraseña"));
    }
}
