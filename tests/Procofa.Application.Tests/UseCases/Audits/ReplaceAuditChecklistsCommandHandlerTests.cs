using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Audits.ReplaceAuditChecklists;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.UseCases.Audits;

public sealed class ReplaceAuditChecklistsCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private sealed record Fixture(
        ReplaceAuditChecklistsCommandHandler Handler,
        FakeAuditChecklistRepository AuditChecklists,
        Audit Audit,
        FakeChecklistRepository Checklists,
        FakeChecklistVersionRepository Versions);

    private static Audit CreateSeedAudit()
    {
        var audit = new Audit(
            Guid.NewGuid(), TenantId, "AUD-SEED-0003", Guid.NewGuid(), Guid.NewGuid(), null,
            InMemoryAuditTypeCatalog.InternaOea.Id, InMemoryProfileCatalog.Maquila.Id, Guid.NewGuid(), "Objetivo",
            "Alcance", null, new DateOnly(2026, 3, 1), UserId, ExecutionMode.Remote);

        audit.ReplacePrograms([InMemoryProgramCatalog.Oea.Id]);
        return audit;
    }

    private static Fixture CreateHandler(params Checklist[] checklists)
    {
        var audit = CreateSeedAudit();
        var audits = new FakeAuditRepository(audit);
        var checklistRepo = new FakeChecklistRepository(checklists);
        var versionRepo = new FakeChecklistVersionRepository();
        var auditChecklists = new FakeAuditChecklistRepository();

        var handler = new ReplaceAuditChecklistsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits, checklistRepo, versionRepo,
            auditChecklists);

        return new Fixture(handler, auditChecklists, audit, checklistRepo, versionRepo);
    }

    private static ChecklistVersion PublishedVersion(Guid tenantId, Guid checklistId, Guid userId)
    {
        var version = new ChecklistVersion(Guid.NewGuid(), tenantId, checklistId, 1, userId);
        version.Publish(DateTime.UtcNow);
        return version;
    }

    [Fact]
    public async Task ReplaceAuditChecklists_ChecklistPublicadoCompatible_Asocia()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Checklist Compatible", null, UserId);
        var fixture = CreateHandler(checklist);
        var version = PublishedVersion(TenantId, checklist.Id, UserId);
        var versions = new FakeChecklistVersionRepository(version);

        var handler = new ReplaceAuditChecklistsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(fixture.Audit),
            fixture.Checklists, versions, fixture.AuditChecklists);

        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(fixture.Audit.Id, [checklist.Id]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var associated = Assert.Single(fixture.AuditChecklists.AuditChecklists);
        Assert.Equal(version.Id, associated.ChecklistVersionId);
    }

    [Fact]
    public async Task ReplaceAuditChecklists_ChecklistSoloEnDraft_Conflicto()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Checklist Sin Publicar", null, UserId);
        var fixture = CreateHandler(checklist);
        // Ninguna versión publicada sembrada en fixture.Versions (DRAFT implícito: no existe fila PUBLISHED).

        var result = await fixture.Handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(fixture.Audit.Id, [checklist.Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditChecklistsError.NoPublishedVersion, result.Error);
        Assert.Empty(fixture.AuditChecklists.AuditChecklists);
    }

    [Fact]
    public async Task ReplaceAuditChecklists_ProgramaIncompatible_Conflicto()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Ctpat.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist CTPAT", null, UserId);
        var fixture = CreateHandler(checklist);
        var version = PublishedVersion(TenantId, checklist.Id, UserId);
        var versions = new FakeChecklistVersionRepository(version);

        var handler = new ReplaceAuditChecklistsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(fixture.Audit),
            fixture.Checklists, versions, fixture.AuditChecklists);

        // audit.Programs solo contiene OEA — CTPAT no pertenece a la auditoría.
        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(fixture.Audit.Id, [checklist.Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditChecklistsError.IncompatibleChecklist, result.Error);
    }

    [Fact]
    public async Task ReplaceAuditChecklists_AuditTypeGenericoComoFallback_Asocia()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist Genérico", null, UserId);
        var fixture = CreateHandler(checklist);
        var version = PublishedVersion(TenantId, checklist.Id, UserId);
        var versions = new FakeChecklistVersionRepository(version);

        var handler = new ReplaceAuditChecklistsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(fixture.Audit),
            fixture.Checklists, versions, fixture.AuditChecklists);

        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(fixture.Audit.Id, [checklist.Id]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(fixture.AuditChecklists.AuditChecklists);
    }

    [Fact]
    public async Task ReplaceAuditChecklists_GenericoConExactoPublicadoDisponible_Conflicto()
    {
        var exactChecklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Checklist Exacto", null, UserId);
        var genericChecklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist Genérico", null, UserId);

        var fixture = CreateHandler(exactChecklist, genericChecklist);
        var exactVersion = PublishedVersion(TenantId, exactChecklist.Id, UserId);
        var genericVersion = PublishedVersion(TenantId, genericChecklist.Id, UserId);
        var versions = new FakeChecklistVersionRepository(exactVersion, genericVersion);

        var handler = new ReplaceAuditChecklistsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(fixture.Audit),
            fixture.Checklists, versions, fixture.AuditChecklists);

        // Existe un checklist EXACTO activo y publicado para el mismo Program+Profile+AuditType de
        // la auditoría: el genérico deja de ser aplicable como fallback y se rechaza, aunque venga
        // elegido por id explícito — el genérico nunca desplaza a un exacto disponible.
        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(fixture.Audit.Id, [genericChecklist.Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditChecklistsError.IncompatibleChecklist, result.Error);
        Assert.Empty(fixture.AuditChecklists.AuditChecklists);
    }

    [Fact]
    public async Task ReplaceAuditChecklists_DosExactosUnoDraftUnoPublicado_GenericoSigueRechazado()
    {
        // Sin UNIQUE de BD sobre (program, profile, audit_type_id): el primer candidato exacto
        // (más reciente) puede estar en DRAFT sin que eso habilite al genérico — debe seguirse
        // probando hasta el segundo candidato exacto, que sí está PUBLISHED.
        var exactDraft = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Exacto Draft Más Reciente", null, UserId);
        var exactPublished = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Exacto Publicado Más Antiguo", null, UserId);
        var genericChecklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist Genérico", null, UserId);

        typeof(Checklist).GetProperty(nameof(Checklist.CreatedAtUtc))!.SetValue(exactDraft, DateTime.UtcNow);
        typeof(Checklist).GetProperty(nameof(Checklist.CreatedAtUtc))!
            .SetValue(exactPublished, DateTime.UtcNow.AddDays(-1));

        var fixture = CreateHandler(exactDraft, exactPublished, genericChecklist);
        var draftVersion = new ChecklistVersion(Guid.NewGuid(), TenantId, exactDraft.Id, 1, UserId);
        var publishedVersion = PublishedVersion(TenantId, exactPublished.Id, UserId);
        var genericVersion = PublishedVersion(TenantId, genericChecklist.Id, UserId);
        var versions = new FakeChecklistVersionRepository(draftVersion, publishedVersion, genericVersion);

        var handler = new ReplaceAuditChecklistsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(fixture.Audit),
            fixture.Checklists, versions, fixture.AuditChecklists);

        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(fixture.Audit.Id, [genericChecklist.Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditChecklistsError.IncompatibleChecklist, result.Error);
        Assert.Empty(fixture.AuditChecklists.AuditChecklists);
    }

    [Fact]
    public async Task ReplaceAuditChecklists_ChecklistInactivo_Conflicto()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Checklist Inactivo", null, UserId);
        checklist.Deactivate();
        var fixture = CreateHandler(checklist);
        var version = PublishedVersion(TenantId, checklist.Id, UserId);
        var versions = new FakeChecklistVersionRepository(version);

        var handler = new ReplaceAuditChecklistsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(fixture.Audit),
            fixture.Checklists, versions, fixture.AuditChecklists);

        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(fixture.Audit.Id, [checklist.Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditChecklistsError.IncompatibleChecklist, result.Error);
        Assert.Empty(fixture.AuditChecklists.AuditChecklists);
    }

    [Fact]
    public async Task ReplaceAuditChecklists_ExactoDisponible_SeleccionarExactoDirectamenteOk()
    {
        var exactChecklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Checklist Exacto", null, UserId);
        var genericChecklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist Genérico", null, UserId);

        var fixture = CreateHandler(exactChecklist, genericChecklist);
        var exactVersion = PublishedVersion(TenantId, exactChecklist.Id, UserId);
        var genericVersion = PublishedVersion(TenantId, genericChecklist.Id, UserId);
        var versions = new FakeChecklistVersionRepository(exactVersion, genericVersion);

        var handler = new ReplaceAuditChecklistsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(fixture.Audit),
            fixture.Checklists, versions, fixture.AuditChecklists);

        // El exacto nunca se ve afectado por la existencia del genérico — solo el genérico está
        // condicionado a la ausencia de un exacto aplicable.
        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(fixture.Audit.Id, [exactChecklist.Id]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var associated = Assert.Single(fixture.AuditChecklists.AuditChecklists);
        Assert.Equal(exactVersion.Id, associated.ChecklistVersionId);
    }
}
