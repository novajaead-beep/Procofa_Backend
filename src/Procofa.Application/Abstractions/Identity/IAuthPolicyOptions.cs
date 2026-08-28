namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// Política de autenticación resuelta desde configuración. Application consume solo estos valores
/// ya resueltos — nunca lee <c>IConfiguration</c> directamente (se mantiene sin dependencias de
/// Infrastructure/Api). </summary>
public interface IAuthPolicyOptions
{
    /// <summary>Intentos fallidos consecutivos antes de aplicar lockout.</summary>
    int MaxFailedLoginAttempts { get; }

    /// <summary>Duración del lockout una vez alcanzado <see cref="MaxFailedLoginAttempts"/>.</summary>
    TimeSpan LockoutDuration { get; }

    /// <summary>Vigencia del refresh token emitido en login.</summary>
    TimeSpan RefreshTokenLifetime { get; }
}
