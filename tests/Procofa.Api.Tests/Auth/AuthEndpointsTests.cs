using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Procofa.Api.Contracts.Auth;
using Procofa.Infrastructure.Security;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.Api.Tests.Auth;

/// <summary>
/// Tests HTTP end-to-end del módulo Auth completo (<c>login</c>/<c>refresh</c>/<c>logout</c>/
/// <c>me</c>), vía <see cref="WebApplicationFactory{TEntryPoint}"/> contra la app REAL
/// (<c>Procofa.Api.Program</c>) apuntando al PostgreSQL desechable de
/// <see cref="PostgresBaselineFixture"/>.
///
/// A diferencia del cliente de login simple, este suite maneja las cookies MANUALMENTE (sin
/// <c>HandleCookies</c>) para poder ejercer casos que un <see cref="HttpClient"/> con manejo
/// automático de cookies no puede expresar: reenviar una cookie ya rotada, una cookie con valor
/// inválido, o directamente ninguna cookie. El valor crudo de la cookie nunca se imprime, ni se usa
/// en el nombre/mensaje de un assert — solo se reenvía tal cual entre requests.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class AuthEndpointsTests : IAsyncLifetime
{
    private const string RefreshCookieName = "procofa_refresh";

    private readonly PostgresBaselineFixture _fixture;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public AuthEndpointsTests(PostgresBaselineFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient Client =>
        _client ?? throw new InvalidOperationException("El cliente HTTP todavía no fue inicializado.");

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
                    ["Auth:RefreshCookie:Name"] = RefreshCookieName,
                    ["Auth:RefreshCookie:Secure"] = "false",
                    ["Auth:RefreshCookie:SameSite"] = "Strict",
                    ["Auth:RefreshCookie:Path"] = "/api/auth",
                });
            });
        });

        // HandleCookies = false: este suite controla el envío de la cookie de refresh
        // explícitamente en cada request (vía RequestWithCookie), para poder reenviar valores ya
        // rotados/inválidos/ausentes a voluntad. Con el default (HandleCookies = true) el
        // CookieContainer interno del handler captura la cookie más reciente devuelta por el
        // servidor y la reenvía automáticamente, pisando cualquier header "Cookie" agregado a
        // mano — lo que hacía indetectable un intento de reuso de un refresh token ya rotado.
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
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

    private static string? ExtractSetCookieLine(HttpResponseMessage response, string cookieName) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith($"{cookieName}=", StringComparison.OrdinalIgnoreCase))
            : null;

    private static string ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        var line = ExtractSetCookieLine(response, cookieName)
            ?? throw new InvalidOperationException($"La respuesta no trae Set-Cookie para '{cookieName}'.");

        var afterName = line[(cookieName.Length + 1)..];
        var end = afterName.IndexOf(';');
        return end >= 0 ? afterName[..end] : afterName;
    }

    private HttpRequestMessage RequestWithCookie(HttpMethod method, string path, string? cookieValue)
    {
        var request = new HttpRequestMessage(method, path);
        if (cookieValue is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookieName}={cookieValue}");
        }

        return request;
    }

    private async Task<(HttpResponseMessage Response, string Email, string RefreshCookie, string AccessToken)>
        LoginNewUserAsync(params string[] roleCodes)
    {
        var password = $"clave-{Guid.NewGuid():N}";
        var passwordHash = new PasswordHasherAdapter().HashPassword(password);
        var email = $"auth-endpoints.{Guid.NewGuid():N}@procofa-test.invalid";

        await _fixture.CreateUserWithPasswordAsync(
            AuthHandlerFactory.ProcofaTenantId, email, passwordHash, roleCodes.Length > 0 ? roleCodes : ["AUDITOR_LIDER"]);

        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var refreshCookie = ExtractCookieValue(response, RefreshCookieName);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        return (response, email, refreshCookie, body!.AccessToken);
    }

    private async Task SetUserActiveAsync(string email, bool isActive)
    {
        await using var connection = await _fixture.OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE public.users SET is_active = @isActive WHERE email = @email;";
        command.Parameters.AddWithValue("isActive", isActive);
        command.Parameters.AddWithValue("email", email);
        await command.ExecuteNonQueryAsync();
    }

    // ---- POST /api/auth/login ----

    [Fact]
    public async Task PostLogin_ConCredencialesValidas_Devuelve200_SinRefreshTokenEnElBody_YCookieConformeAlContrato()
    {
        var (response, _, _, _) = await LoginNewUserAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("refreshToken", rawBody, StringComparison.OrdinalIgnoreCase);

        var setCookie = ExtractSetCookieLine(response, RefreshCookieName);
        Assert.NotNull(setCookie);
        Assert.Contains($"{RefreshCookieName}=", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    // ---- POST /api/auth/refresh ----

    [Fact]
    public async Task PostRefresh_ConCookieValida_Devuelve200_NuevoAccessTokenYNuevaCookie_YLaAnteriorQuedaInutilizable()
    {
        var (_, _, oldRefreshCookie, oldAccessToken) = await LoginNewUserAsync();

        using var refreshRequest = RequestWithCookie(HttpMethod.Post, "/api/auth/refresh", oldRefreshCookie);
        var refreshResponse = await Client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(refreshBody);
        Assert.NotEqual(oldAccessToken, refreshBody!.AccessToken);

        var newRefreshCookie = ExtractCookieValue(refreshResponse, RefreshCookieName);
        Assert.NotEqual(oldRefreshCookie, newRefreshCookie);

        // El token anterior, ya rotado, no puede reutilizarse.
        using var reuseRequest = RequestWithCookie(HttpMethod.Post, "/api/auth/refresh", oldRefreshCookie);
        var reuseResponse = await Client.SendAsync(reuseRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task PostRefresh_SinCookie_Devuelve401_YEliminaCookie()
    {
        using var request = RequestWithCookie(HttpMethod.Post, "/api/auth/refresh", cookieValue: null);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var setCookie = ExtractSetCookieLine(response, RefreshCookieName);
        Assert.NotNull(setCookie);
    }

    [Fact]
    public async Task PostRefresh_ConCookieInvalida_Devuelve401()
    {
        using var request = RequestWithCookie(HttpMethod.Post, "/api/auth/refresh", "un-valor-que-nunca-fue-emitido");
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostRefresh_ConCookieRevocada_Devuelve401()
    {
        var (_, _, refreshCookie, _) = await LoginNewUserAsync();

        using var logoutRequest = RequestWithCookie(HttpMethod.Post, "/api/auth/logout", refreshCookie);
        var logoutResponse = await Client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var refreshRequest = RequestWithCookie(HttpMethod.Post, "/api/auth/refresh", refreshCookie);
        var refreshResponse = await Client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    // ---- POST /api/auth/logout ----

    [Fact]
    public async Task PostLogout_ConCookieValida_Devuelve204_EliminaCookie_YRefreshPosteriorFalla()
    {
        var (_, _, refreshCookie, _) = await LoginNewUserAsync();

        using var logoutRequest = RequestWithCookie(HttpMethod.Post, "/api/auth/logout", refreshCookie);
        var logoutResponse = await Client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        var setCookie = ExtractSetCookieLine(logoutResponse, RefreshCookieName);
        Assert.NotNull(setCookie);

        using var refreshRequest = RequestWithCookie(HttpMethod.Post, "/api/auth/refresh", refreshCookie);
        var refreshResponse = await Client.SendAsync(refreshRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task PostLogout_SinCookie_Devuelve204()
    {
        using var request = RequestWithCookie(HttpMethod.Post, "/api/auth/logout", cookieValue: null);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ---- GET /api/auth/me ----

    [Fact]
    public async Task GetMe_ConJwtValido_Devuelve200()
    {
        var (_, email, _, accessToken) = await LoginNewUserAsync("AUDITOR_LIDER");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.NotNull(me);
        Assert.Equal(email, me!.Email);
        Assert.Contains("AUDITOR_LIDER", me.Roles);
    }

    [Fact]
    public async Task GetMe_SinJwt_Devuelve401()
    {
        var response = await Client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_ConUsuarioInactivo_Devuelve401()
    {
        var (_, email, _, accessToken) = await LoginNewUserAsync("CONSULTOR");
        await SetUserActiveAsync(email, isActive: false);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
