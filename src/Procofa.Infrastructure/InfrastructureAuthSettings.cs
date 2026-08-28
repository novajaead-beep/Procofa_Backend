namespace Procofa.Infrastructure;

/// <summary>
/// Valores de configuración de Auth ya resueltos como primitivos — mismo principio que
/// <c>AddInfrastructure(string? connectionString)</c>: Infrastructure nunca depende de
/// <c>IConfiguration</c> directamente, es <c>Procofa.Api/Program.cs</c> (el Composition Root real)
/// quien conoce la forma de la configuración (appsettings/environment/user-secrets) y arma este
/// record antes de llamar <c>AddInfrastructure</c>. </summary> <param name="ProcofaTenantId">
/// Tenant Stage 1 fijo (sección "TENANT STAGE 1" de la instrucción):
/// <c>00000000-0000-0000-0000-000000000001</c>. Configurable (no hardcodeado en Infrastructure)
/// para no romper si algún entorno de prueba necesita otro valor, pero el default esperado en Api
/// es ese GUID. </param> <param name="JwtIssuer">Claim <c>iss</c> del JWT de acceso.</param> <param
/// name="JwtAudience">Claim <c>aud</c> del JWT de acceso.</param> <param name="JwtSigningKey">
/// Clave simétrica de firma HMAC-SHA256 — NUNCA hardcodeada; viene de environment/user-secrets.
/// Debe tener al menos 32 bytes (256 bits) una vez codificada a UTF-8; <see
/// cref="Security.JwtAccessTokenGenerator"/> valida esto en construcción (fail-fast, "Config
/// validation al startup"). </param> <param name="JwtAccessTokenMinutes">Vigencia del access token,
/// en minutos.</param> <param name="AuthMaxFailedLoginAttempts">Intentos fallidos consecutivos
/// antes de lockout.</param> <param name="AuthLockoutMinutes">Duración del lockout, en
/// minutos.</param> <param name="AuthRefreshTokenDays">Vigencia del refresh token, en días.</param>
public sealed record InfrastructureAuthSettings(
    Guid ProcofaTenantId,
    string JwtIssuer,
    string JwtAudience,
    string JwtSigningKey,
    int JwtAccessTokenMinutes,
    int AuthMaxFailedLoginAttempts,
    int AuthLockoutMinutes,
    int AuthRefreshTokenDays);
