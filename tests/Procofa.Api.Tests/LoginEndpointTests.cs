using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Procofa.Api.Contracts.Auth;
using Procofa.Infrastructure.Security;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.Api.Tests;

/// <summary>
/// Tests HTTP end-to-end de <c>POST /api/auth/login</c>, vía <see
/// cref="WebApplicationFactory{TEntryPoint}"/> contra la app REAL (<c>Procofa.Api.Program</c>)
/// apuntando al PostgreSQL desechable de <see cref="PostgresBaselineFixture"/>. </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class LoginEndpointTests : IAsyncLifetime
{
    private readonly PostgresBaselineFixture _fixture;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    private HttpClient Client =>
    _client ??
    throw new InvalidOperationException(
        "El cliente HTTP todavía no fue inicializado.");
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
                    ["Auth:RefreshCookie:Name"] = "procofa_refresh",
                    ["Auth:RefreshCookie:Secure"] = "false",
                    ["Auth:RefreshCookie:SameSite"] = "Strict",
                    ["Auth:RefreshCookie:Path"] = "/api/auth",
                });
            });
        });

        _client = _factory.CreateClient(
     new WebApplicationFactoryClientOptions
     {
         HandleCookies = true,
     });
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
    public async Task PostLogin_ConCredencialesValidas_Devuelve200YCookieHttpOnly()
    {
        var passwordHash =
            new PasswordHasherAdapter()
                .HashPassword(
                    "una-contraseña-segura-de-api-tests");

        var email =
            $"api-login-ok.{Guid.NewGuid():N}@procofa-test.invalid";

        await _fixture.CreateUserWithPasswordAsync(
            AuthHandlerFactory.ProcofaTenantId,
            email,
            passwordHash,
            "AUDITOR_LIDER");

        var response =
            await _client!.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password =
                        "una-contraseña-segura-de-api-tests",
                });

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var body =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(body);

        Assert.False(
            string.IsNullOrWhiteSpace(
                body!.AccessToken));

        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            value =>
                value.Contains(
                    "procofa_refresh=",
                    StringComparison.OrdinalIgnoreCase) &&
                value.Contains(
                    "httponly",
                    StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public async Task PostLogout_RevocaRefreshToken()
    {
        var password =
            "una-contraseña-segura-logout";

        var passwordHash =
            new PasswordHasherAdapter()
                .HashPassword(password);

        var email =
            $"api-logout.{Guid.NewGuid():N}@procofa-test.invalid";

        await _fixture.CreateUserWithPasswordAsync(
            AuthHandlerFactory.ProcofaTenantId,
            email,
            passwordHash,
            "AUDITOR_LIDER");

        var loginResponse =
            await _client!.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password,
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var logoutResponse =
            await _client.PostAsync(
                "/api/auth/logout",
                content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        var refreshResponse =
            await _client.PostAsync(
                "/api/auth/refresh",
                content: null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            refreshResponse.StatusCode);
    }

    [Fact]
    public async Task GetMe_ConAccessTokenValido_DevuelveUsuario()
    {
        var password =
            "una-contraseña-segura-me";

        var passwordHash =
            new PasswordHasherAdapter()
                .HashPassword(password);

        var email =
            $"api-me.{Guid.NewGuid():N}@procofa-test.invalid";

        await _fixture.CreateUserWithPasswordAsync(
            AuthHandlerFactory.ProcofaTenantId,
            email,
            passwordHash,
            "AUDITOR_LIDER");

        var loginResponse =
            await _client!.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password,
                });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginBody =
            await loginResponse.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginBody);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginBody!.AccessToken);

        var meResponse =
            await _client.GetAsync(
                "/api/auth/me");

        Assert.Equal(
            HttpStatusCode.OK,
            meResponse.StatusCode);

        var me =
            await meResponse.Content
                .ReadFromJsonAsync<CurrentUserResponse>();

        Assert.NotNull(me);

        Assert.Equal(
            email,
            me!.Email);

        Assert.Contains(
            "AUDITOR_LIDER",
            me.Roles);
    }
}
