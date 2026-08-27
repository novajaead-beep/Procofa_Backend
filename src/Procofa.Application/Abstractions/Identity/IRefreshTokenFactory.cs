namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// Par generado para un nuevo refresh token: el valor crudo (se entrega al
/// cliente una única vez, nunca se persiste) y su hash (lo único que se
/// guarda en <c>refresh_tokens.token_hash</c>).
/// </summary>
/// <param name="RawToken">Valor crudo del token — entregado al cliente, nunca persistido ni logueado.</param>
/// <param name="TokenHash">Hash del valor crudo (SHA-256 o equivalente) — el único valor que se persiste.</param>
public sealed record GeneratedRefreshToken(string RawToken, string TokenHash);

/// <summary>
/// Puerto de generación de refresh tokens (Instrucción 04, sección "REFRESH
/// TOKEN"): valor criptográficamente seguro vía
/// <c>System.Security.Cryptography.RandomNumberGenerator</c>, nunca
/// <c>System.Random</c>. La implementación concreta vive en Infrastructure.
/// </summary>
public interface IRefreshTokenFactory
{
    GeneratedRefreshToken Create();
}
