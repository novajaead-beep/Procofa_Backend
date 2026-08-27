using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Procofa.Api.Contracts.Auth;
using Procofa.Infrastructure.Security;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.Api.Tests;

/// <summary>
/// Tests HTTP end-to-end de <c>POST /api/auth/login</c> (Instrucción 04,
/// sección "TESTS MÍNIMOS" → Api: "200" / "401" / "400 ProblemDetails"),
/// vía <see cref="WebApplicationFactory{TEntryPoint}"/> contra la app REAL
/// (<c>Procofa.Api.Program</c>) apuntando al PostgreSQL desechable de
/// <see cref="PostgresBaselineFixture"/>. NO ejecutados por Claude en este
/// sandbox (Docker inalcanzable) — mismo caso que
/// <c>Procofa.IntegrationTests</c>, cuya fixture se reutiliza aquí.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class LoginEndpointTests : IAsyncLifetime
{
    private readonly PostgresBaselineFixture _fixture;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public LoginEndpointTests(PostgresBaselineFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:ProcofaDb"] = _fixture.AppConnectionString,
                    ["Tenancy:ProcofaTenantId"] = AuthHandlerFactory.ProcofaTenantId.ToString(),
                    ["Jwt:Issuer"] = "procofa-api-tests",
                    ["Jwt:Audience"] = "procofa-api-tests",
                    ["Jwt:SigningKey"] = "clave-de-firma-de-pruebas-api-de-al-menos-32-bytes",
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["Auth:MaxFailedLoginAttempts"] = "5",
                    ["Auth:LockoutMinutes"] = "15",
                    ["Auth:RefreshTokenDays"] = "30",
                });
            });
        });

        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task PostLogin_ConCredencialesValidas_Devuelve200ConTokens()
    {
        var passwordHash = new PasswordHasherAdapter().HashPassword("una-contraseña-segura-de-api-tests");
        var email = $"api-login-ok.{Guid.NewGuid():N}@procofa-test.invalid";
        await _fixture.CreateUserWithPasswordAsync(
            AuthHandlerFactory.ProcofaTenantId, email, passwordHash, "AUDITOR_LIDER");

        var response = await _client!.PostAsJsonAsync(
            "/api/auth/login", new { email, password = "una-contraseña-segura-de-api-tests" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
    }

    [Fact]
    public async Task PostLogin_ConCredencialesIncorrectas_Devuelve401Generico()
    {
        var response = await _client!.PostAsJsonAsync(
            "/api/auth/login", new { email = "no-existe@procofa-test.invalid", password = "lo-que-sea" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostLogin_ConRequestInvalido_Devuelve400ProblemDetails()
    {
        var response = await _client!.PostAsJsonAsync("/api/auth/login", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
