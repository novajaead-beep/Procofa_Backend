namespace Procofa.Application.UseCases.Auth.Login;

/// <summary>
/// Motivo de un login fallido. Existe para trazabilidad interna/tests — Api SIEMPRE debe traducir
/// cualquiera de estos tres valores a la MISMA respuesta HTTP genérica (401): "no revelar si el
/// email existe" y "usar respuesta uniforme para credenciales inválidas" aplican incluso entre <see
/// cref="InvalidCredentials"/>, <see cref="UserInactive"/> y <see cref="UserLocked"/> — la
/// distinción solo debe ser visible del lado servidor (access_logs, tests), nunca en el body de la
/// respuesta. </summary>
public enum LoginError
{
    /// <summary>Usuario inexistente en el tenant, o contraseña incorrecta — ambos casos colapsan aquí a propósito.</summary>
    InvalidCredentials,

    /// <summary><c>IsActive = false</c>.</summary>
    UserInactive,

    /// <summary><c>LockedUntilUtc</c> vigente.</summary>
    UserLocked,
}
