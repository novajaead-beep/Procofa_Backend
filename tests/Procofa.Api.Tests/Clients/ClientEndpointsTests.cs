using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Procofa.Api.Contracts.Clients;
using Procofa.Api.Contracts.Companies;
using Procofa.Api.Contracts.Contacts;
using Procofa.Api.Contracts.Sites;
using Procofa.Api.Tests.Users;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.Api.Tests.Clients;

[Collection(PostgresBaselineCollection.Name)]
public sealed class ClientEndpointsTests : IAsyncLifetime
{
    private readonly PostgresBaselineFixture _fixture;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public ClientEndpointsTests(PostgresBaselineFixture fixture)
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
    public async Task GetClients_SinJwt_Devuelve401()
    {
        var response = await _client!.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetClients_ConAdmin_Devuelve200()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ClientListResponse>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task GetClients_ConAuditorLider_Devuelve200()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "AUDITOR_LIDER"));

        var response = await _client!.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetClients_ConRolCliente_SoloDevuelveLosAsignados()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var clienteUserId = Guid.NewGuid();
        var email = $"cliente-scope.{clienteUserId:N}@procofa-test.invalid";
        await SeedClienteUserAsync(tenantId, clienteUserId, email);

        var visibleClientId = await _fixture.CreateClientAsync(tenantId, "Cliente Visible Para CLIENTE");
        await _fixture.CreateClientAsync(tenantId, "Cliente No Asignado");
        await GrantClientAccessAsync(tenantId, clienteUserId, visibleClientId);

        AuthorizeAs(UserEndpointsTestSupport.CreateToken(clienteUserId, "CLIENTE"));

        var response = await _client!.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ClientListResponse>();
        Assert.NotNull(body);
        Assert.All(body!.Items, item => Assert.Equal(visibleClientId, item.Id));
        Assert.Contains(body.Items, item => item.Id == visibleClientId);
    }

    [Fact]
    public async Task PostClients_ConAdmin_Devuelve201()
    {
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, AuthHandlerFactory.ProcofaTenantId, adminId, $"admin-clients.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Comercializadora del Bajío", null, null, null, null, null, ["OEA"]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateClientResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
    }

    [Fact]
    public async Task PostClients_ConAuditorLider_Devuelve403()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "AUDITOR_LIDER"));

        var response = await _client!.PostAsJsonAsync(
            "/api/clients",
            new CreateClientRequest("Cliente Sin Permiso", null, null, null, null, null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetClient_Inexistente_Devuelve404()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync($"/api/clients/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostCompanies_ConClientValido_Devuelve201()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var clientId = await _fixture.CreateClientAsync(tenantId, "Cliente Para Empresa");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/clients/{clientId}/companies",
            new CreateCompanyRequest(null, "Empresa Auditada de Prueba", null, null, null, null, false));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateCompanyResponse>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task PostCompanies_ConClientFueraDeLaRuta_Devuelve404()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/clients/{Guid.NewGuid()}/companies",
            new CreateCompanyRequest(null, "Empresa Sin Client", null, null, null, null, false));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostSites_ConCompanyValida_Devuelve201()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var clientId = await _fixture.CreateClientAsync(tenantId, "Cliente Para Sede");
        var companyId = await _fixture.CreateAuditedCompanyAsync(tenantId, clientId, "Empresa Para Sede");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/clients/{clientId}/companies/{companyId}/sites",
            new CreateSiteRequest("Sede Principal", "Calle Falsa 123", null, null, null, null, null, null, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateSiteResponse>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task PostSites_ConCompanyFueraDeLaEmpresa_Devuelve404()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var clientId = await _fixture.CreateClientAsync(tenantId, "Cliente A Para Sede Cruzada");
        var otherClientId = await _fixture.CreateClientAsync(tenantId, "Cliente B Para Sede Cruzada");
        var companyOfOtherClient = await _fixture.CreateAuditedCompanyAsync(tenantId, otherClientId, "Empresa De Otro Cliente");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/clients/{clientId}/companies/{companyOfOtherClient}/sites",
            new CreateSiteRequest("Sede Cruzada", "Calle Falsa 456", null, null, null, null, null, null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostContacts_ConClientValido_Devuelve201()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var clientId = await _fixture.CreateClientAsync(tenantId, "Cliente Para Contacto");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/clients/{clientId}/contacts",
            new CreateContactRequest(null, "Mario", "Torres", null, "mario@procofa-test.invalid", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateContactResponse>();
        Assert.NotNull(body);
    }

    private async Task SeedClienteUserAsync(Guid tenantId, Guid userId, string email)
    {
        var clienteRoleId = await _fixture.GetCatalogIdByCodeAsync("roles", "CLIENTE");

        await using var connection = await _fixture.OpenSuperuserConnectionAsync();

        await using (var userCommand = connection.CreateCommand())
        {
            userCommand.CommandText = """
                INSERT INTO public.users (id, tenant_id, email, password_hash, first_name, last_name, is_active)
                VALUES (@id, @tenantId, @email, @passwordHash, 'Test', 'Cliente', true);
                """;
            userCommand.Parameters.AddWithValue("id", userId);
            userCommand.Parameters.AddWithValue("tenantId", tenantId);
            userCommand.Parameters.AddWithValue("email", email);
            userCommand.Parameters.AddWithValue("passwordHash", "test-only-not-a-real-hash");
            await userCommand.ExecuteNonQueryAsync();
        }

        await using (var roleCommand = connection.CreateCommand())
        {
            roleCommand.CommandText = """
                INSERT INTO public.user_roles (tenant_id, user_id, role_id)
                VALUES (@tenantId, @userId, @roleId);
                """;
            roleCommand.Parameters.AddWithValue("tenantId", tenantId);
            roleCommand.Parameters.AddWithValue("userId", userId);
            roleCommand.Parameters.AddWithValue("roleId", clienteRoleId);
            await roleCommand.ExecuteNonQueryAsync();
        }
    }

    private async Task GrantClientAccessAsync(Guid tenantId, Guid userId, Guid clientId)
    {
        await using var connection = await _fixture.OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO public.user_client_access (tenant_id, user_id, client_id)
            VALUES (@tenantId, @userId, @clientId);
            """;
        command.Parameters.AddWithValue("tenantId", tenantId);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("clientId", clientId);
        await command.ExecuteNonQueryAsync();
    }
}
