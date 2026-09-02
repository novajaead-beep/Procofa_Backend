using Microsoft.EntityFrameworkCore;
using Npgsql;
using Procofa.Application.UseCases.Audits.CreateAudit;
using Procofa.Application.UseCases.Audits.GetAudit;
using Procofa.Application.UseCases.Audits.ListAudits;
using Procofa.Application.UseCases.Audits.ReplaceAuditChecklists;
using Procofa.Application.UseCases.Audits.ReplaceAuditPrograms;
using Procofa.Application.UseCases.Audits.ReplaceAuditTeam;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests.Audits;

/// <summary>
/// Tests de integración de planificación de auditorías contra PostgreSQL 18 real vía
/// Testcontainers, corriendo el grafo REAL de Infrastructure (<see cref="AuditsHandlerFactory"/>)
/// como <c>procofa_app</c>. La verificación física usa <c>SuperuserConnectionString</c>
/// únicamente en la fase de assert.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class AuditsManagementIntegrationTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task CreateAudit_Onsite_PersisteAuditYAuditPrograms()
    {
        var tenantId = await fixture.CreateTenantAsync("audits-create");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-audit-create");
        var clientId = await fixture.CreateClientAsync(tenantId, "Cliente Auditado");
        var companyId = await fixture.CreateAuditedCompanyAsync(tenantId, clientId, "Empresa Auditada");
        var siteId = await fixture.CreateCompanySiteAsync(tenantId, companyId, "Sede Principal");
        var auditTypeId = await fixture.GetCatalogIdByCodeAsync("audit_types", "INTERNA_OEA");
        var profileId = await fixture.GetCatalogIdByCodeAsync("profiles", "MAQUILA");

        var (handler, dbContext) = AuditsHandlerFactory.CreateCreateAuditHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateAuditCommand(
                clientId, companyId, siteId, auditTypeId, profileId, ["OEA"], "Objetivo de prueba",
                "Alcance de prueba", null, new DateOnly(2026, 6, 1), "ONSITE"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.AuditId);
        Assert.StartsWith("AUD-", result.Folio);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.Audits
            .Include(a => a.Programs)
            .SingleAsync(a => a.Id == result.AuditId);

        Assert.Equal(clientId, persisted.ClientId);
        Assert.Equal(siteId, persisted.CompanySiteId);
        Assert.Single(persisted.Programs);
        Assert.True(persisted.IsEditable);
    }

    [Fact]
    public async Task ReplaceAuditPrograms_ReemplazaLaColeccionCompleta()
    {
        var tenantId = await fixture.CreateTenantAsync("audits-programs");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-audit-programs");
        var auditData = await fixture.CreateMinimalAuditAsync(tenantId, admin, "PROGRAMS-1");

        var (handler, dbContext) = AuditsHandlerFactory.CreateReplaceAuditProgramsHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new ReplaceAuditProgramsCommand(auditData.AuditId, ["OEA", "CTPAT"]), CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.Audits.Include(a => a.Programs).SingleAsync(a => a.Id == auditData.AuditId);
        Assert.Equal(2, persisted.Programs.Count);
    }

    [Fact]
    public async Task ReplaceAuditTeam_ConLead_PersisteElEquipo()
    {
        var tenantId = await fixture.CreateTenantAsync("audits-team");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-audit-team");
        var lead = await fixture.CreateUserAsync(tenantId, "lead-audit-team");
        var support = await fixture.CreateUserAsync(tenantId, "support-audit-team");
        var auditData = await fixture.CreateMinimalAuditAsync(tenantId, admin, "TEAM-1");

        var (handler, dbContext) = AuditsHandlerFactory.CreateReplaceAuditTeamHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new ReplaceAuditTeamCommand(
                auditData.AuditId,
                [new ReplaceAuditTeamMemberInput(lead, "LEAD"), new ReplaceAuditTeamMemberInput(support, "SUPPORT")]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.Audits.Include(a => a.Team).SingleAsync(a => a.Id == auditData.AuditId);
        Assert.Equal(2, persisted.Team.Count);
        Assert.Contains(persisted.Team, m => m.UserId == lead && m.AssignedByUserId == admin);
    }

    [Fact]
    public async Task ReplaceAuditChecklists_VersionPublicadaCompatible_PersisteAuditChecklist()
    {
        var tenantId = await fixture.CreateTenantAsync("audits-checklists");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-audit-checklists");
        var auditTypeId = await fixture.GetCatalogIdByCodeAsync("audit_types", "INTERNA_OEA");
        var auditData = await fixture.CreateMinimalAuditAsync(tenantId, admin, "CHECKLISTS-1");

        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist Auditoría", auditTypeId);
        var versionId = await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        await fixture.PublishChecklistVersionDirectAsync(versionId);

        var oeaId = await fixture.GetCatalogIdByCodeAsync("programs", "OEA");
        await using (var connection = await fixture.OpenSuperuserConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO public.audit_programs (tenant_id, audit_id, program_id)
                VALUES (@tenantId, @auditId, @programId);
                """;
            command.Parameters.AddWithValue("tenantId", tenantId);
            command.Parameters.AddWithValue("auditId", auditData.AuditId);
            command.Parameters.AddWithValue("programId", oeaId);
            await command.ExecuteNonQueryAsync();
        }

        var (handler, dbContext) = AuditsHandlerFactory.CreateReplaceAuditChecklistsHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(auditData.AuditId, [checklistId]), CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.AuditChecklists.SingleAsync(ac => ac.AuditId == auditData.AuditId);
        Assert.Equal(versionId, persisted.ChecklistVersionId);
    }

    [Fact]
    public async Task ReplaceAuditChecklists_VersionEnDraft_EsRechazado()
    {
        var tenantId = await fixture.CreateTenantAsync("audits-checklists-draft");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-audit-checklists-draft");
        var auditData = await fixture.CreateMinimalAuditAsync(tenantId, admin, "CHECKLISTS-DRAFT-1");

        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist Draft");
        await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);

        var oeaId = await fixture.GetCatalogIdByCodeAsync("programs", "OEA");
        await using (var connection = await fixture.OpenSuperuserConnectionAsync())
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO public.audit_programs (tenant_id, audit_id, program_id)
                VALUES (@tenantId, @auditId, @programId);
                """;
            command.Parameters.AddWithValue("tenantId", tenantId);
            command.Parameters.AddWithValue("auditId", auditData.AuditId);
            command.Parameters.AddWithValue("programId", oeaId);
            await command.ExecuteNonQueryAsync();
        }

        var (handler, dbContext) = AuditsHandlerFactory.CreateReplaceAuditChecklistsHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(auditData.AuditId, [checklistId]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditChecklistsError.NoPublishedVersion, result.Error);
    }

    [Fact]
    public async Task GetAudit_DeOtroTenant_RlsLoHaceInvisible_DevuelveNotFound()
    {
        var tenantId = await fixture.CreateTenantAsync("audits-rls-a");
        var otherTenantId = await fixture.CreateTenantAsync("audits-rls-b");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-audit-rls");
        var otherAdmin = await fixture.CreateUserAsync(otherTenantId, "admin-audit-rls-other");
        var auditOfOtherTenant = await fixture.CreateMinimalAuditAsync(otherTenantId, otherAdmin, "RLS-OTHER");

        var (handler, dbContext) = AuditsHandlerFactory.CreateGetAuditHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(new GetAuditQuery(auditOfOtherTenant.AuditId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetAuditError.NotFound, result.Error);
    }

    [Fact]
    public async Task ListAudits_SoloDevuelveLosDelTenantActivo()
    {
        var tenantId = await fixture.CreateTenantAsync("audits-list-rls-a");
        var otherTenantId = await fixture.CreateTenantAsync("audits-list-rls-b");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-audit-list");
        var otherAdmin = await fixture.CreateUserAsync(otherTenantId, "admin-audit-list-other");
        var auditOfTenant = await fixture.CreateMinimalAuditAsync(tenantId, admin, "LIST-A");
        var auditOfOtherTenant = await fixture.CreateMinimalAuditAsync(otherTenantId, otherAdmin, "LIST-B");

        var (handler, dbContext) = AuditsHandlerFactory.CreateListAuditsHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new ListAuditsQuery(null, null, null, null, null, null, 1, 25), CancellationToken.None);

        var visibleIds = result.Items.Select(i => i.Id).ToList();
        Assert.Contains(auditOfTenant.AuditId, visibleIds);
        Assert.DoesNotContain(auditOfOtherTenant.AuditId, visibleIds);
    }

    [Fact]
    public async Task CreateAudit_ConClientIdInexistente_ProduceErrorDeAplicacion()
    {
        var tenantId = await fixture.CreateTenantAsync("audits-fk-invalid-client");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-audit-fk");

        var (handler, dbContext) = AuditsHandlerFactory.CreateCreateAuditHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateAuditCommand(
                Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), [], "Objetivo", "Alcance",
                null, new DateOnly(2026, 6, 1), "REMOTE"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateAuditError.ClientNotFound, result.Error);
    }

    [Fact]
    public async Task InsertAuditDirecto_ConAuditTypeIdInvalido_EsRechazadoPorFk()
    {
        var tenantId = await fixture.CreateTenantAsync("audits-fk-raw");
        var admin = await fixture.CreateUserAsync(tenantId, "admin-audit-fk-raw");
        var clientId = await fixture.CreateClientAsync(tenantId, "Cliente FK Raw");
        var companyId = await fixture.CreateAuditedCompanyAsync(tenantId, clientId, "Empresa FK Raw");
        var profileId = await fixture.GetCatalogIdByCodeAsync("profiles", "MAQUILA");
        var statusId = await fixture.GetCatalogIdByCodeAsync("audit_statuses", "BORRADOR");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var setTenant = connection.CreateCommand();
        setTenant.CommandText = "SELECT set_config('app.tenant_id', @tenantId, false);";
        setTenant.Parameters.AddWithValue("tenantId", tenantId.ToString());
        await setTenant.ExecuteNonQueryAsync();

        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO public.audits (
                id, tenant_id, folio, client_id, audited_company_id, audit_type_id,
                profile_id, status_id, objective, scope, scheduled_date,
                created_by_user_id, execution_mode)
            VALUES (
                @id, @tenantId, @folio, @clientId, @companyId, @auditTypeId,
                @profileId, @statusId, 'Objetivo', 'Alcance', CURRENT_DATE,
                @createdByUserId, 'REMOTE');
            """;
        insert.Parameters.AddWithValue("id", Guid.NewGuid());
        insert.Parameters.AddWithValue("tenantId", tenantId);
        insert.Parameters.AddWithValue("folio", "AUD-FK-RAW-1");
        insert.Parameters.AddWithValue("clientId", clientId);
        insert.Parameters.AddWithValue("companyId", companyId);
        insert.Parameters.AddWithValue("auditTypeId", Guid.NewGuid());
        insert.Parameters.AddWithValue("profileId", profileId);
        insert.Parameters.AddWithValue("statusId", statusId);
        insert.Parameters.AddWithValue("createdByUserId", admin);

        await Assert.ThrowsAsync<PostgresException>(() => insert.ExecuteNonQueryAsync());
    }
}
