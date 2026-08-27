using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Procofa.Application.Abstractions.Identity;

namespace Procofa.Infrastructure.Security;

/// <summary>
/// Implementación de <see cref="IAccessTokenGenerator"/> (Instrucción 04,
/// sección "JWT"). Firma HMAC-SHA256 con una clave simétrica —
/// suficiente para Etapa 1 (un solo servicio emite y valida). Claims
/// mínimos exigidos por la instrucción: <c>sub</c>, <c>tenant_id</c>,
/// <c>email</c>, <c>roles</c> (uno por rol — nunca un único claim con la
/// lista serializada, para que <c>ClaimsPrincipal.IsInRole</c> funcione sin
/// parseo adicional el día que se agregue el middleware de autorización).
/// Se agrega también <c>jti</c> (id único del token, sección "AUTH ADR" del
/// hilo previo — soporta futura revocación/blacklist, aunque esta
/// instrucción no la implementa) e <c>iat</c>/<c>exp</c> estándar, ambos en UTC.
/// </summary>
public sealed class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    private const int MinimumSigningKeyBytes = 32; // 256 bits — mínimo recomendado para HS256.

    private readonly string _issuer;
    private readonly string _audience;
    private readonly SigningCredentials _signingCredentials;
    private readonly TimeSpan _accessTokenLifetime;

    public JwtAccessTokenGenerator(string issuer, string audience, string signingKey, int accessTokenMinutes)
    {
        // "Config validation al startup": fail-fast aquí en vez de fallar
        // silenciosamente en el primer login real.
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer no está configurado.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Jwt:Audience no está configurado.");
        }

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey no está configurado. Defínalo vía variable de entorno o " +
                "'dotnet user-secrets set Jwt:SigningKey ...' en desarrollo — nunca en appsettings.json.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        if (keyBytes.Length < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey debe tener al menos {MinimumSigningKeyBytes} bytes UTF-8 " +
                $"({MinimumSigningKeyBytes * 8} bits) para HS256; tiene {keyBytes.Length}.");
        }

        if (accessTokenMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:AccessTokenMinutes debe ser mayor a 0.");
        }

        _issuer = issuer;
        _audience = audience;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        _accessTokenLifetime = TimeSpan.FromMinutes(accessTokenMinutes);
    }

    public AccessToken GenerateAccessToken(
        Guid userId, Guid tenantId, string email, IReadOnlyCollection<string> roleCodes)
    {
        var nowUtc = DateTime.UtcNow;
        var expiresAtUtc = nowUtc.Add(_accessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("tenant_id", tenantId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(nowUtc).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        claims.AddRange(roleCodes.Select(roleCode => new Claim("roles", roleCode)));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresAtUtc,
            signingCredentials: _signingCredentials);

        var value = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessToken(value, expiresAtUtc);
    }
}
