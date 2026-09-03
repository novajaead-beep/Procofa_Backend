using Microsoft.EntityFrameworkCore;
using Procofa.Application.UseCases.Checklists.CreateChecklist;
using Procofa.Application.UseCases.Checklists.ResolveChecklist;
using Procofa.Application.UseCases.Checklists.UpdateChecklist;
using Procofa.Application.UseCases.ChecklistSections.CreateChecklistSection;
using Procofa.Application.UseCases.ChecklistSections.DeleteChecklistSection;
using Procofa.Application.UseCases.ChecklistVersions.CreateChecklistVersion;
using Procofa.Application.UseCases.ChecklistVersions.PublishChecklistVersion;
using Procofa.Application.UseCases.ChecklistVersions.UpdateChecklistVersion;
using Procofa.Application.UseCases.Criteria.CreateCriterion;
using Procofa.Domain.Enums;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests.Checklists;

/// <summary>
/// Tests de integración del módulo de plantillas de checklist contra PostgreSQL 18 real vía
/// Testcontainers, corriendo el grafo REAL de Infrastructure (<see cref="ChecklistsHandlerFactory"/>)
/// como <c>procofa_app</c>. La verificación física usa <c>SuperuserConnectionString</c> únicamente
/// en la fase de assert — mismo patrón que <see cref="Clients.ClientsManagementIntegrationTests"/>.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class ChecklistsManagementIntegrationTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task CreateChecklist_PersisteBajoElTenantCorrecto()
    {
        var tenantId = await fixture.CreateTenantAsync("checklists-create");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-checklists-create");
        var programId = await fixture.GetCatalogIdByCodeAsync("programs", "OEA");
        var profileId = await fixture.GetCatalogIdByCodeAsync("profiles", "MAQUILA");

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateCreateChecklistHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateChecklistCommand(programId, profileId, null, "Checklist OEA Maquila", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.Checklists.SingleAsync(c => c.Id == result.ChecklistId);
        Assert.Equal(tenantId, persisted.TenantId);
        Assert.True(persisted.IsActive);
    }

    [Fact]
    public async Task CreateVersion_AsignaVersionNumberSecuencial()
    {
        var tenantId = await fixture.CreateTenantAsync("versions-sequential");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-versions");
        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist Versionado");
        await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateCreateChecklistVersionHandler(
            fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateChecklistVersionCommand(checklistId, "Segunda versión"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.VersionNumber);
    }

    [Fact]
    public async Task PublishVersion_ConSeccionesYCriterios_PersisteEstadoPublicado()
    {
        var tenantId = await fixture.CreateTenantAsync("versions-publish");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-publish");
        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist a Publicar");
        var versionId = await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        var sectionId = await fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección 1");
        await fixture.CreateCriterionAsync(tenantId, sectionId, "CRIT-1", "¿Cumple?");

        var (handler, dbContext) = ChecklistsHandlerFactory.CreatePublishChecklistVersionHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new PublishChecklistVersionCommand(checklistId, versionId), CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.ChecklistVersions.SingleAsync(v => v.Id == versionId);
        Assert.Equal(ChecklistVersionStatus.Published, persisted.Status);
        Assert.NotNull(persisted.PublishedAtUtc);
    }

    [Fact]
    public async Task PublishVersion_SinCriterios_NoPersisteCambioDeEstado()
    {
        var tenantId = await fixture.CreateTenantAsync("versions-publish-no-criteria");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-publish-empty");
        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist Vacío");
        var versionId = await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        await fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección Sin Criterios");

        var (handler, dbContext) = ChecklistsHandlerFactory.CreatePublishChecklistVersionHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new PublishChecklistVersionCommand(checklistId, versionId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PublishChecklistVersionError.NoCriteria, result.Error);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.ChecklistVersions.SingleAsync(v => v.Id == versionId);
        Assert.Equal(ChecklistVersionStatus.Draft, persisted.Status);
    }

    [Fact]
    public async Task UpdateVersion_SobrePublicada_NoPersisteCambio_ApplicationEnforzaInmutabilidad()
    {
        var tenantId = await fixture.CreateTenantAsync("versions-immutable");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-immutable");
        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist Inmutable");
        var versionId = await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        await fixture.PublishChecklistVersionDirectAsync(versionId);

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateUpdateChecklistVersionHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new UpdateChecklistVersionCommand(checklistId, versionId, "Intento de cambio"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateChecklistVersionError.VersionPublished, result.Error);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.ChecklistVersions.SingleAsync(v => v.Id == versionId);
        Assert.Null(persisted.ChangeNotes);
    }

    [Fact]
    public async Task CreateSection_SobreVersionPublicada_NoPersisteLaSeccion()
    {
        var tenantId = await fixture.CreateTenantAsync("sections-on-published");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-sections-published");
        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist con Versión Publicada");
        var versionId = await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        await fixture.PublishChecklistVersionDirectAsync(versionId);

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateCreateChecklistSectionHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateChecklistSectionCommand(checklistId, versionId, null, "Sección Tardía", null, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateChecklistSectionError.VersionPublished, result.Error);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        Assert.False(await verifyContext.ChecklistSections.AnyAsync(s => s.ChecklistVersionId == versionId));
    }

    [Fact]
    public async Task CreateCriterion_ConCodeDuplicadoEnLaMismaSeccion_EsRechazadoPorLaUnicaFisica()
    {
        var tenantId = await fixture.CreateTenantAsync("criteria-unique-code");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-criteria-unique");
        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist Criterios");
        var versionId = await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        var sectionId = await fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección");
        await fixture.CreateCriterionAsync(tenantId, sectionId, "CRIT-DUP", "¿Primera?");

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateCreateCriterionHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateCriterionCommand(
                checklistId, versionId, sectionId, "CRIT-DUP", "¿Duplicada?", null, null, null,
                ImportanceLevel.Media, null, null, true, 2),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCriterionError.CodeAlreadyExists, result.Error);
    }

    [Fact]
    public async Task DeleteSection_ConCriteriosAsociados_LaBdRestringeYElHandlerDevuelveConflictoLimpio()
    {
        var tenantId = await fixture.CreateTenantAsync("sections-restrict-delete");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-restrict");
        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist Restrict");
        var versionId = await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        var sectionId = await fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección Con Criterio");
        await fixture.CreateCriterionAsync(tenantId, sectionId, "CRIT-1", "¿Pregunta?");

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateDeleteChecklistSectionHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new DeleteChecklistSectionCommand(checklistId, versionId, sectionId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteChecklistSectionError.HasCriteria, result.Error);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        Assert.True(await verifyContext.ChecklistSections.AnyAsync(s => s.Id == sectionId));
    }

    [Fact]
    public async Task Resolve_ConCoincidenciaExacta_DevuelveLaVersionPublicadaCorrecta()
    {
        var tenantId = await fixture.CreateTenantAsync("resolve-exact");
        var admin = await fixture.CreateUserAsync(tenantId, "admin-resolve-exact");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var auditTypeId = await fixture.GetCatalogIdByCodeAsync("audit_types", "INTERNA_OEA");
        var checklistId = await fixture.CreateChecklistAsync(
            tenantId, admin, "Checklist Específico", auditTypeId);
        var versionId = await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        var sectionId = await fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección");
        await fixture.CreateCriterionAsync(tenantId, sectionId, "CRIT-1", "¿Pregunta?");
        await fixture.PublishChecklistVersionDirectAsync(versionId);

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateResolveChecklistHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", "INTERNA_OEA"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsExactMatch);
        Assert.Equal(checklistId, result.ChecklistId);
        Assert.Equal(versionId, result.VersionId);
    }

    [Fact]
    public async Task Resolve_SinCoincidenciaExacta_CaeAlChecklistGenericoDelTenant()
    {
        var tenantId = await fixture.CreateTenantAsync("resolve-fallback");
        var admin = await fixture.CreateUserAsync(tenantId, "admin-resolve-fallback");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var checklistId = await fixture.CreateChecklistAsync(tenantId, admin, "Checklist Genérico");
        var versionId = await fixture.CreateChecklistVersionAsync(tenantId, checklistId, admin, 1);
        var sectionId = await fixture.CreateChecklistSectionAsync(tenantId, versionId, "Sección");
        await fixture.CreateCriterionAsync(tenantId, sectionId, "CRIT-1", "¿Pregunta?");
        await fixture.PublishChecklistVersionDirectAsync(versionId);

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateResolveChecklistHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", "INTERNA_OEA"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsExactMatch);
        Assert.Equal(checklistId, result.ChecklistId);
    }

    [Fact]
    public async Task Resolve_DeOtroTenant_RlsLoHaceInvisible_DevuelveNotFound()
    {
        var tenantId = await fixture.CreateTenantAsync("resolve-rls-a");
        var otherTenantId = await fixture.CreateTenantAsync("resolve-rls-b");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var otherAdmin = await fixture.CreateUserAsync(otherTenantId, "admin-other-tenant");
        var checklistId = await fixture.CreateChecklistAsync(otherTenantId, otherAdmin, "Checklist de Otro Tenant");
        var versionId = await fixture.CreateChecklistVersionAsync(otherTenantId, checklistId, otherAdmin, 1);
        var sectionId = await fixture.CreateChecklistSectionAsync(otherTenantId, versionId, "Sección");
        await fixture.CreateCriterionAsync(otherTenantId, sectionId, "CRIT-1", "¿Pregunta?");
        await fixture.PublishChecklistVersionDirectAsync(versionId);

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateResolveChecklistHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResolveChecklistError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateChecklist_ConChecklistDeOtroTenant_RlsLoHaceInvisible_DevuelveNotFound()
    {
        var tenantId = await fixture.CreateTenantAsync("checklists-rls-update-a");
        var otherTenantId = await fixture.CreateTenantAsync("checklists-rls-update-b");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var otherAdmin = await fixture.CreateUserAsync(otherTenantId, "admin-update-other");
        var checklistDeOtroTenant = await fixture.CreateChecklistAsync(
            otherTenantId, otherAdmin, "Checklist de Otro Tenant");
        var programId = await fixture.GetCatalogIdByCodeAsync("programs", "OEA");
        var profileId = await fixture.GetCatalogIdByCodeAsync("profiles", "MAQUILA");

        var (handler, dbContext) = ChecklistsHandlerFactory.CreateUpdateChecklistHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new UpdateChecklistCommand(checklistDeOtroTenant, programId, profileId, null, "Intento", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateChecklistError.NotFound, result.Error);
    }
}
