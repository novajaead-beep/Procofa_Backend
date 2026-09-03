using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Procofa.Api.Contracts.Users;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;
namespace Procofa.Api.Tests.Users;

using Microsoft.Extensions.Configuration;


[Collection(PostgresBaselineCollection.Name)]
public sealed class UserEndpointsTests : IAsyncLifetime
{
    private readonly PostgresBaselineFixture _fixture;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public UserEndpointsTests(PostgresBaselineFixture fixture)
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
                    ["Jwt:Issuer"] = UserEndpointsTestSupport.JwtIssuer,
                    ["Jwt:Audience"] = UserEndpointsTestSupport.JwtAudience,
                    ["Jwt:SigningKey"] = UserEndpointsTestSupport.JwtSigningKey,
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

    private void AuthorizeAs(string token) =>
        _client!.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task GetUsers_SinJwt_Devuelve401()
    {
        var response = await _client!.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ConJwtSinRolAdmin_Devuelve403()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "CONSULTOR"));

        var response = await _client!.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ConJwtAdmin_Devuelve200()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserListResponse>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task PostUsers_ConAdminValido_Devuelve201()
    {
        // CreateUser persiste user_roles.assigned_by_user_id = ICurrentUser.UserId (el "sub" del
        // JWT) — enforce_same_tenant_references() exige que ese id exista físicamente en "users"
        // del mismo tenant, así que el admin autenticado debe sembrarse primero (no basta con un
        // sub arbitrario válido solo para la autorización HTTP).
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, AuthHandlerFactory.ProcofaTenantId, adminId, $"admin-post.{adminId:N}@procofa-test.invalid");

        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));
        var email = $"nuevo.{Guid.NewGuid():N}@procofa-test.invalid";

        var response = await _client!.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(email, "Nuevo", "Usuario", null, "PasswordTemporalSeguro123!", ["AUDITOR_APOYO"], []));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
    }

    [Fact]
    public async Task PostUsers_ConRequestInvalido_Devuelve400()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        // Sin roles (mínimo un rol es obligatorio) y sin temporaryPassword.
        var response = await _client!.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest("invalido@procofa-test.invalid", "X", "Y", null, null, [], []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchStatus_UnAdminSobreSuPropiaCuenta_Devuelve409()
    {
        var email = $"admin-self.{Guid.NewGuid():N}@procofa-test.invalid";
        var adminId = await _fixture.CreateUserWithPasswordAsync(
            AuthHandlerFactory.ProcofaTenantId, email, "hash-de-prueba", "ADMIN");

        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var response = await _client!.PatchAsJsonAsync(
            $"/api/users/{adminId}/status", new ChangeUserStatusRequest(IsActive: false));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_UsuarioInexistente_Devuelve404()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
