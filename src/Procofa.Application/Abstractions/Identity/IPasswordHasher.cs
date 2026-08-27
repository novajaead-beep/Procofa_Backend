namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// Resultado de verificar una contraseña contra un hash (Instrucción 04).
/// Espejo propio, libre de Infrastructure, del resultado que produce
/// <c>Microsoft.AspNetCore.Identity.PasswordHasher&lt;TUser&gt;</c> — la
/// implementación real vive en Infrastructure (<c>PasswordHasherAdapter</c>)
/// y mapea 1 a 1 hacia este tipo, para que Application nunca referencie el
/// paquete de Identity directamente.
/// </summary>
public enum PasswordVerificationResult
{
    /// <summary>La contraseña no coincide con el hash.</summary>
    Failed,

    /// <summary>La contraseña coincide; el hash sigue usando parámetros vigentes.</summary>
    Success,

    /// <summary>
    /// La contraseña coincide, pero el hash fue creado con parámetros
    /// desactualizados (ej. iteraciones antiguas) — el caso de uso debe
    /// recalcular el hash con <see cref="IPasswordHasher.HashPassword"/> y
    /// persistirlo antes de terminar la operación.
    /// </summary>
    SuccessRehashNeeded,
}

/// <summary>
/// Puerto de hashing de contraseñas (Instrucción 04, sección "ARQUITECTURA":
/// "Infrastructure implementa persistencia, hashing/JWT si corresponde").
/// Application orquesta el caso de uso sin conocer el algoritmo concreto.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Genera un nuevo hash a partir de una contraseña en texto plano. Nunca loggear el resultado ni el input.</summary>
    string HashPassword(string password);

    /// <summary>Verifica <paramref name="providedPassword"/> contra <paramref name="passwordHash"/> ya persistido.</summary>
    PasswordVerificationResult VerifyPassword(string passwordHash, string providedPassword);
}
