using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Procofa.Api.Contracts.Audits;
using Procofa.Api.Tests.Users;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.Api.Tests.Audits;

[Collection(PostgresBaselineCollection.Name)]
public sealed class AuditEndpointsTests : IAsyncLifetime
{
    private readonly PostgresBaselineFixture _fixture;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public AuditEndpointsTests(PostgresBaselineFixture fixture)
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

    private async Task<(Guid ClientId, Guid CompanyId, Guid SiteId, Guid AuditTypeId, Guid ProfileId)>
        SeedPlanningPrerequisitesAsync(Guid tenantId, string suffix)
    {
        var clientId = await _fixture.CreateClientAsync(tenantId, $"Cliente API {suffix}");
        var companyId = await _fixture.CreateAuditedCompanyAsync(tenantId, clientId, $"Empresa API {suffix}");
        var siteId = await _fixture.CreateCompanySiteAsync(tenantId, companyId, $"Sede API {suffix}");
        var auditTypeId = await _fixture.GetCatalogIdByCodeAsync("audit_types", "INTERNA_OEA");
        var profileId = await _fixture.GetCatalogIdByCodeAsync("profiles", "MAQUILA");

        return (clientId, companyId, siteId, auditTypeId, profileId);
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

    [Fact]
    public async Task GetAudits_SinJwt_Devuelve401()
    {
        var response = await _client!.GetAsync("/api/audits");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAudits_ConAdmin_Devuelve200()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync("/api/audits");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAudits_ConAuditorLider_Devuelve200()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "AUDITOR_LIDER"));

        var response = await _client!.GetAsync("/api/audits");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostAudits_ConAdmin_Devuelve201()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "post-201");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-post.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo API", "Alcance API", null,
                new DateOnly(2026, 6, 1), "ONSITE"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateAuditResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
    }

    [Fact]
    public async Task PostAudits_OnsiteSinSitio_Devuelve400()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, _, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "post-400");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-post-400.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var response = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, null, auditTypeId, profileId, ["OEA"], "Objetivo API", "Alcance API", null,
                new DateOnly(2026, 6, 1), "ONSITE"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostAudits_ConAuditorLider_Devuelve403()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "post-403");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "AUDITOR_LIDER"));

        var response = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo API", "Alcance API", null,
                new DateOnly(2026, 6, 1), "ONSITE"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAudit_Inexistente_Devuelve404()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync($"/api/audits/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAudit_ClienteAsignado_Devuelve200()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "get-cliente-ok");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-get-cliente.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, [], "Objetivo Cliente", "Alcance Cliente", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var clienteUserId = Guid.NewGuid();
        var email = $"cliente-audit.{clienteUserId:N}@procofa-test.invalid";
        await SeedClienteUserAsync(tenantId, clienteUserId, email);
        await GrantClientAccessAsync(tenantId, clienteUserId, clientId);
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(clienteUserId, "CLIENTE"));

        var response = await _client!.GetAsync($"/api/audits/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAudit_ClienteFueraDeAlcance_Devuelve404()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "get-cliente-404");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-get-cliente-404.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, [], "Objetivo Sin Acceso", "Alcance Sin Acceso",
                null, new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var clienteUserId = Guid.NewGuid();
        var email = $"cliente-audit-404.{clienteUserId:N}@procofa-test.invalid";
        await SeedClienteUserAsync(tenantId, clienteUserId, email);
        // Sin GrantClientAccessAsync: el CLIENTE no tiene el client de esta auditoría asignado.
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(clienteUserId, "CLIENTE"));

        var response = await _client!.GetAsync($"/api/audits/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutPrograms_ConAdmin_Devuelve204()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "put-programs");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-programs.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}/programs", new ReplaceAuditProgramsRequest(["OEA", "CTPAT"]));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutTeam_ConAdmin_Devuelve204()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "put-team");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-team.{adminId:N}@procofa-test.invalid");
        var leadId = await _fixture.CreateUserAsync(tenantId, "lead-audit-team-api");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, [], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}/team",
            new ReplaceAuditTeamRequest([new AuditTeamMemberRequest(leadId, "LEAD")]));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutTeam_ConDosLead_Devuelve409()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "put-team-dos-lead");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-team-dos-lead.{adminId:N}@procofa-test.invalid");
        var leadAId = await _fixture.CreateUserAsync(tenantId, "lead-a-audit-team-api");
        var leadBId = await _fixture.CreateUserAsync(tenantId, "lead-b-audit-team-api");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, [], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}/team",
            new ReplaceAuditTeamRequest(
                [new AuditTeamMemberRequest(leadAId, "LEAD"), new AuditTeamMemberRequest(leadBId, "LEAD")]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PutChecklists_ConChecklistPublicadoCompatible_Devuelve204()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "put-checklists-ok");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-checklists.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var checklistId = await _fixture.CreateChecklistAsync(tenantId, adminId, "Checklist API", auditTypeId);
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, adminId, 1);
        await _fixture.PublishChecklistVersionDirectAsync(versionId);

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}/checklists", new ReplaceAuditChecklistsRequest([checklistId]));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutChecklists_ConChecklistEnDraft_Devuelve409()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "put-checklists-draft");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-checklists-draft.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var checklistId = await _fixture.CreateChecklistAsync(tenantId, adminId, "Checklist Draft API", auditTypeId);
        await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, adminId, 1);

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}/checklists", new ReplaceAuditChecklistsRequest([checklistId]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetAudit_DespuesDePutChecklists_MuestraElChecklistAsignado()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "get-checklists");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-get-checklists.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var checklistId = await _fixture.CreateChecklistAsync(
            tenantId, adminId, "Checklist GET API", auditTypeId);
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, adminId, 1);
        await _fixture.PublishChecklistVersionDirectAsync(versionId);

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var putResponse = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}/checklists", new ReplaceAuditChecklistsRequest([checklistId]));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var response = await _client!.GetAsync($"/api/audits/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuditDetailResponse>();
        Assert.NotNull(body);
        var checklistItem = Assert.Single(body!.Checklists);
        Assert.Equal(checklistId, checklistItem.ChecklistId);
        Assert.Equal(versionId, checklistItem.ChecklistVersionId);
    }

    [Fact]
    public async Task GetAudits_ExecutionModeInvalido_Devuelve400()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.GetAsync("/api/audits?executionMode=INVALIDO");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAudit_ConAdmin_Devuelve204()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "put-audit-ok");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-put-ok.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}",
            new UpdateAuditRequest(
                companyId, siteId, auditTypeId, profileId, "Objetivo actualizado", "Alcance actualizado", null,
                new DateOnly(2026, 7, 1), "REMOTE"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutAudit_ConRolSoloLectura_Devuelve403()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "put-audit-403");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-put-403.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "CONSULTOR"));

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}",
            new UpdateAuditRequest(
                companyId, siteId, auditTypeId, profileId, "Objetivo", "Alcance", null,
                new DateOnly(2026, 7, 1), "REMOTE"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutAudit_AuditoriaInexistente_Devuelve404()
    {
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(Guid.NewGuid(), "ADMIN"));

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{Guid.NewGuid()}",
            new UpdateAuditRequest(
                Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), "Objetivo", "Alcance", null,
                new DateOnly(2026, 7, 1), "REMOTE"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutAudit_CambioDeProfileRompeChecklistAsignado_Devuelve409()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "put-audit-checklist-409");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-put-checklist-409.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        var checklistId = await _fixture.CreateChecklistAsync(
            tenantId, adminId, "Checklist PUT API", auditTypeId);
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, adminId, 1);
        await _fixture.PublishChecklistVersionDirectAsync(versionId);

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var putChecklistsResponse = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}/checklists", new ReplaceAuditChecklistsRequest([checklistId]));
        Assert.Equal(HttpStatusCode.NoContent, putChecklistsResponse.StatusCode);

        var transportistaProfileId = await _fixture.GetCatalogIdByCodeAsync("profiles", "TRANSPORTISTA");

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{created.Id}",
            new UpdateAuditRequest(
                companyId, siteId, auditTypeId, transportistaProfileId, "Objetivo", "Alcance", null,
                new DateOnly(2026, 7, 1), "ONSITE"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PutChecklists_ConChecklistIncompatible_Devuelve409()
    {
        var tenantId = AuthHandlerFactory.ProcofaTenantId;
        var (clientId, companyId, siteId, auditTypeId, profileId) =
            await SeedPlanningPrerequisitesAsync(tenantId, "put-checklists-incompatible");
        var adminId = Guid.NewGuid();
        await UserEndpointsTestSupport.SeedAdminAsync(
            _fixture, tenantId, adminId, $"admin-audit-checklists-incompatible.{adminId:N}@procofa-test.invalid");
        AuthorizeAs(UserEndpointsTestSupport.CreateToken(adminId, "ADMIN"));

        // Checklist bajo CTPAT/TRANSPORTISTA — la auditoría solo tiene el programa OEA asociado.
        var checklistId = await _fixture.CreateChecklistAsync(
            tenantId, adminId, "Checklist Incompatible API", programCode: "CTPAT", profileCode: "TRANSPORTISTA");
        var versionId = await _fixture.CreateChecklistVersionAsync(tenantId, checklistId, adminId, 1);
        await _fixture.PublishChecklistVersionDirectAsync(versionId);

        var createResponse = await _client!.PostAsJsonAsync(
            "/api/audits",
            new CreateAuditRequest(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo", "Alcance", null,
                new DateOnly(2026, 6, 1), "ONSITE"));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateAuditResponse>();

        var response = await _client!.PutAsJsonAsync(
            $"/api/audits/{created!.Id}/checklists", new ReplaceAuditChecklistsRequest([checklistId]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
