namespace Procofa.Api.Contracts.Auth;

/// <summary>
/// Respuesta 200 de <c>POST /api/auth/login</c>.
///
/// Nota deliberada de alcance: el mecanismo de entrega del refresh token
/// (body JSON, como aquí, vs. cookie HttpOnly) fue explícitamente pausado
/// por el usuario antes de esta instrucción ("detente" sobre el ADR de
/// dominios/CORS/cookie) y NO fue retomado — esta instrucción no lo
/// resuelve por su cuenta. Se entrega en el body como placeholder mínimo
/// funcional; migrar a cookie es un cambio de Api aislado (no toca
/// Application/Domain) cuando esa ADR se cierre.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    IReadOnlyCollection<string> Roles);
