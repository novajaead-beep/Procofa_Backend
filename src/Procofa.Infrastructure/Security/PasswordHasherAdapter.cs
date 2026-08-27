using Microsoft.AspNetCore.Identity;
using Procofa.Application.Abstractions.Identity;
using Procofa.Domain.Entities.Identity;
using DomainPasswordVerificationResult = Procofa.Application.Abstractions.Identity.PasswordVerificationResult;
using IdentityPasswordVerificationResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace Procofa.Infrastructure.Security;

/// <summary>
/// Implementación de <see cref="IPasswordHasher"/> vía
/// <see cref="PasswordHasher{TUser}"/> de ASP.NET Core Identity (Instrucción
/// 04, sección "LOGIN" paso 7: "Validar contraseña usando
/// PasswordHasher&lt;TUser&gt; o abstracción equivalente de Microsoft").
/// <see cref="Domain.Entities.Identity.User"/> se usa directamente como
/// <c>TUser</c> — <c>PasswordHasher&lt;TUser&gt;</c> no exige que el tipo
/// implemente ninguna interfaz, así que no hace falta un wrapper/DTO
/// adicional. El algoritmo (PBKDF2, parámetros por defecto de Identity) y
/// sus iteraciones son responsabilidad exclusiva de este paquete — Application
/// nunca los conoce.
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string HashPassword(string password) =>
        _inner.HashPassword(null!, password);

    public DomainPasswordVerificationResult VerifyPassword(string passwordHash, string providedPassword)
    {
        var result = _inner.VerifyHashedPassword(null!, passwordHash, providedPassword);

        return result switch
        {
            IdentityPasswordVerificationResult.Success => DomainPasswordVerificationResult.Success,
            IdentityPasswordVerificationResult.SuccessRehashNeeded => DomainPasswordVerificationResult.SuccessRehashNeeded,
            IdentityPasswordVerificationResult.Failed => DomainPasswordVerificationResult.Failed,
            _ => throw new ArgumentOutOfRangeException(
                nameof(result), result, $"{nameof(IdentityPasswordVerificationResult)} sin mapeo explícito."),
        };
    }
}
