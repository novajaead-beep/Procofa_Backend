using Procofa.Infrastructure.Security;

namespace Procofa.Infrastructure.Tests;

/// <summary>
/// Tests de <see cref="JwtAccessTokenGenerator"/> (Instrucción 04, sección
/// "JWT"): claims mínimos exigidos y validación de configuración al
/// construir (fail-fast, "Config validation al startup").
/// </summary>
public sealed class JwtAccessTokenGeneratorTests
{
    private const string ValidSigningKey = "una-clave-de-firma-de-al-menos-32-bytes-de-largo!!";

    [Fact]
    public void Constructor_ConSigningKeyDemasiadoCorta_Lanza()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new JwtAccessTokenGenerator("issuer", "audience", "clave-corta", 15));
    }

    [Fact]
    public void Constructor_ConSigningKeyVacia_Lanza()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new JwtAccessTokenGenerator("issuer", "audience", "", 15));
    }

    [Fact]
    public void Constructor_ConIssuerVacio_Lanza()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new JwtAccessTokenGenerator("", "audience", ValidSigningKey, 15));
    }

    [Fact]
    public void Constructor_ConAccessTokenMinutesNoPositivo_Lanza()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new JwtAccessTokenGenerator("issuer", "audience", ValidSigningKey, 0));
    }

    [Fact]
    public void GenerateAccessToken_IncluyeLosClaimsMinimosYExpiraEnElFuturo()
    {
        var generator = new JwtAccessTokenGenerator("procofa-issuer", "procofa-audience", ValidSigningKey, 15);
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var token = generator.GenerateAccessToken(userId, tenantId, "auditor@procofa.com", ["AUDITOR_LIDER", "CONSULTOR"]);

        Assert.False(string.IsNullOrWhiteSpace(token.Value));
        Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);
        Assert.True(token.ExpiresAtUtc <= DateTime.UtcNow.AddMinutes(15).AddSeconds(5));

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token.Value);

        Assert.Equal(userId.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal(tenantId.ToString(), jwt.Claims.Single(c => c.Type == "tenant_id").Value);
        Assert.Equal("auditor@procofa.com", jwt.Claims.Single(c => c.Type == "email").Value);

        var roleClaims = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value).ToArray();
        Assert.Equal(["AUDITOR_LIDER", "CONSULTOR"], roleClaims);
    }
}
