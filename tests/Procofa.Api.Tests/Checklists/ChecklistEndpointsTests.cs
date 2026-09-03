using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Procofa.Api.Contracts.Checklists;
using Procofa.Api.Contracts.ChecklistSections;
using Procofa.Api.Contracts.ChecklistVersions;
using Procofa.Api.Contracts.Criteria;
using Procofa.Api.Tests.Users;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.Api.Tests.Checklists;

[Collection(PostgresBaselineCollection.Name)]
public sealed class ChecklistEndpointsTests : IAsyncLifetime
{
    private readonly PostgresBaselineFixture _fixture;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public ChecklistEndpointsTests(PostgresBaselineFixture fixture)
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

    private async Task<(Guid ProgramId, Guid ProfileId)> GetProgramAndProfileAsync() =>
        (await _fixture.GetCatalogIdByCodeAsync("programs", "OEA"),
            await _fixture.GetCatalogIdByCodeAsync("profiles", "MAQUILA"));

    [Fact]
    public async Task GetChecklists_SinJwt_Devuelve401()
    {
        var response = await _client!.GetAsync("/api/checklists");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetChecklists_ConAdmin_Devuelve200()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync("/api/checklists");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChecklists_ConAuditorLider_Devuelve200()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "AUDITOR_LIDER"));

        var response = await _client!.GetAsync("/api/checklists");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetChecklists_ConRolCliente_Devuelve403()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "CLIENTE"));

        var response = await _client!.GetAsync("/api/checklists");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PostChecklists_ConAdmin_Devuelve201()
    {
        var (programId, profileId) = await GetProgramAndProfileAsync();
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, AuthHandlerFactory.ProcofaTenantId, adminId,
            $"admin-post-checklist.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            "/api/checklists", new CreateChecklistRequest(programId, profileId, null, "Checklist API", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateChecklistResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
    }

    [Fact]
    public async Task PostChecklists_ConAuditorLider_Devuelve403()
    {
        var (programId, profileId) = await GetProgramAndProfileAsync();
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "AUDITOR_LIDER"));

        var response = await _client!.PostAsJsonAsync(
            "/api/checklists", new CreateChecklistRequest(programId, profileId, null, "Sin Permiso", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetChecklist_Inexistente_Devuelve404()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync($"/api/checklists/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostVersion_ConAdmin_Devuelve201()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var admin = await _fixture.CreateUserAsync(tenantId, "admin-version-api");
        var checklistId = await _fixture.CreateChecklistAsync(tenantId, admin, "Checklist Para Versión");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-post-version.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/checklists/{checklistId}/versions", new CreateChecklistVersionRequest(null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateChecklistVersionResponse>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.VersionNumber);
    }

    [Fact]
    public async Task PublishVersion_ConSeccionesYCriterios_Devuelve204()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var admin = await _fixture.CreateUserAsync(tenantId, "admin-publish-api");
        var checklistId = await _fixture.CreateChecklistAsync(tenantId, admin, "Checklist Para Publicar");
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        var sectionId = await _fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección");
        await _fixture.CreateCriterionAsync(tenantId, sectionId, "CRIT-1", "¿Pregunta?");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsync(
            $"/api/checklists/{checklistId}/versions/{versionId}/publish", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutVersion_SobrePublicada_Devuelve409()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var admin = await _fixture.CreateUserAsync(tenantId, "admin-put-published-api");
        var checklistId = await _fixture.CreateChecklistAsync(tenantId, admin, "Checklist Inmutable API");
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        await _fixture.PublishChecklistVersionDirectAsync(versionId);
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PutAsJsonAsync(
            $"/api/checklists/{checklistId}/versions/{versionId}", new UpdateChecklistVersionRequest("Intento"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostSection_EnVersionDraft_Devuelve201()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var admin = await _fixture.CreateUserAsync(tenantId, "admin-section-api");
        var checklistId = await _fixture.CreateChecklistAsync(tenantId, admin, "Checklist Para Sección");
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/checklists/{checklistId}/versions/{versionId}/sections",
            new CreateChecklistSectionRequest(null, "Sección API", null, 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateChecklistSectionResponse>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task PostSection_EnVersionPublicada_Devuelve409()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var admin = await _fixture.CreateUserAsync(tenantId, "admin-section-conflict-api");
        var checklistId = await _fixture.CreateChecklistAsync(tenantId, admin, "Checklist Sección Publicada");
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        await _fixture.PublishChecklistVersionDirectAsync(versionId);
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/checklists/{checklistId}/versions/{versionId}/sections",
            new CreateChecklistSectionRequest(null, "Sección Tardía", null, 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostCriterion_EnVersionDraft_Devuelve201()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var admin = await _fixture.CreateUserAsync(tenantId, "admin-criterion-api");
        var checklistId = await _fixture.CreateChecklistAsync(tenantId, admin, "Checklist Para Criterio");
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        var sectionId = await _fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección Para Criterio");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/checklists/{checklistId}/versions/{versionId}/sections/{sectionId}/criteria",
            new CreateCriterionRequest("C-API-1", "¿Cumple?", null, null, null, "ALTA", null, null, true, 1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateCriterionResponse>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task PostCriterion_EnVersionPublicada_Devuelve409()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var admin = await _fixture.CreateUserAsync(tenantId, "admin-criterion-conflict-api");
        var checklistId = await _fixture.CreateChecklistAsync(tenantId, admin, "Checklist Criterio Publicado");
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        var sectionId = await _fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección");
        await _fixture.PublishChecklistVersionDirectAsync(versionId);
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            $"/api/checklists/{checklistId}/versions/{versionId}/sections/{sectionId}/criteria",
            new CreateCriterionRequest("C-API-2", "¿Cumple?", null, null, null, null, null, null, true, 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetResolve_ConCoincidenciaExacta_Devuelve200()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var admin = await _fixture.CreateUserAsync(tenantId, "admin-resolve-api");
        var auditTypeId = await _fixture.GetCatalogIdByCodeAsync("audit_types", "INTERNA_OEA");
        var checklistId = await _fixture.CreateChecklistAsync(tenantId, admin, "Checklist Resolve API", auditTypeId);
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        var sectionId = await _fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección");
        await _fixture.CreateCriterionAsync(tenantId, sectionId, "C-RESOLVE-1", "¿Pregunta?");
        await _fixture.PublishChecklistVersionDirectAsync(versionId);
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync("/api/checklists/resolve?program=OEA&profile=MAQUILA&auditType=INTERNA_OEA");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResolveChecklistResponse>();
        Assert.NotNull(body);
        Assert.Equal(checklistId, body!.ChecklistId);
        Assert.True(body.IsExactMatch);
    }

    [Fact]
    public async Task GetResolve_SinVersionPublicada_Devuelve404()
    {
        // Combinación Program+Profile exclusiva de este test (TRANSPORTISTA) para no depender del
        // orden de ejecución frente a otros tests que sí publican bajo OEA/MAQUILA en el mismo
        // tenant fijo de Stage 1.
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var admin = await _fixture.CreateUserAsync(tenantId, "admin-resolve-404-api");
        await _fixture.CreateChecklistAsync(
            tenantId, admin, "Checklist Sin Publicar API", profileCode: "TRANSPORTISTA");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync("/api/checklists/resolve?program=OEA&profile=TRANSPORTISTA");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
