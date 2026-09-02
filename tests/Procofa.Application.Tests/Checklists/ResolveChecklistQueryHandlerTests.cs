using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Checklists.ResolveChecklist;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.Checklists;

public sealed class ResolveChecklistQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static (ResolveChecklistQueryHandler Handler, FakeChecklistRepository Checklists,
        FakeChecklistVersionRepository Versions) CreateHandler(
        FakeChecklistRepository? checklists = null, FakeChecklistVersionRepository? versions = null)
    {
        checklists ??= new FakeChecklistRepository();
        versions ??= new FakeChecklistVersionRepository();
        var handler = new ResolveChecklistQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), checklists, versions,
            new FakeProgramRepository(), new FakeProfileRepository(), new FakeAuditTypeRepository());

        return (handler, checklists, versions);
    }

    private static ChecklistVersion PublishedVersion(Guid checklistId, int versionNumber)
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, checklistId, versionNumber, UserId);
        version.Publish(DateTime.UtcNow);
        return version;
    }

    [Fact]
    public async Task Resolve_ConCoincidenciaExacta_DevuelveEsaVersion()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Específico", null, UserId);
        var version = PublishedVersion(checklist.Id, 1);
        var (handler, _, _) = CreateHandler(
            new FakeChecklistRepository(checklist), new FakeChecklistVersionRepository(version));

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", "INTERNA_OEA"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsExactMatch);
        Assert.Equal(checklist.Id, result.ChecklistId);
        Assert.Equal(version.Id, result.VersionId);
    }

    [Fact]
    public async Task Resolve_SinCoincidenciaExacta_CaeAlGenerico()
    {
        var generic = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Genérico", null, UserId);
        var version = PublishedVersion(generic.Id, 1);
        var (handler, _, _) = CreateHandler(
            new FakeChecklistRepository(generic), new FakeChecklistVersionRepository(version));

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", "INTERNA_OEA"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsExactMatch);
        Assert.Equal(generic.Id, result.ChecklistId);
    }

    [Fact]
    public async Task Resolve_ExactoPublicadoYGenericoPublicado_GanaElExacto()
    {
        var exact = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Exacto", null, UserId);
        var generic = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Genérico", null, UserId);
        var exactVersion = PublishedVersion(exact.Id, 1);
        var genericVersion = PublishedVersion(generic.Id, 1);
        var (handler, _, _) = CreateHandler(
            new FakeChecklistRepository(exact, generic),
            new FakeChecklistVersionRepository(exactVersion, genericVersion));

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", "INTERNA_OEA"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsExactMatch);
        Assert.Equal(exact.Id, result.ChecklistId);
    }

    [Fact]
    public async Task Resolve_ExactoSoloDraftYGenericoPublicado_CaeAlGenerico()
    {
        var exact = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Exacto Draft", null, UserId);
        var generic = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Genérico", null, UserId);
        var exactDraft = new ChecklistVersion(Guid.NewGuid(), TenantId, exact.Id, 1, UserId);
        var genericVersion = PublishedVersion(generic.Id, 1);
        var (handler, _, _) = CreateHandler(
            new FakeChecklistRepository(exact, generic),
            new FakeChecklistVersionRepository(exactDraft, genericVersion));

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", "INTERNA_OEA"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsExactMatch);
        Assert.Equal(generic.Id, result.ChecklistId);
    }

    [Fact]
    public async Task Resolve_DosExactosUnoDraftUnoPublicado_ResuelveElPublicadoNuncaElGenerico()
    {
        // Sin UNIQUE de BD sobre (program, profile, audit_type_id): dos checklists activos pueden
        // coincidir con la misma combinación exacta. El primero (más reciente) está en DRAFT — la
        // resolución debe seguir probando candidatos exactos antes de caer al genérico.
        var exactDraft = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Exacto Draft Más Reciente", null, UserId);
        var exactPublished = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Exacto Publicado Más Antiguo", null, UserId);
        var generic = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Genérico", null, UserId);

        typeof(Checklist).GetProperty(nameof(Checklist.CreatedAtUtc))!
            .SetValue(exactDraft, DateTime.UtcNow);
        typeof(Checklist).GetProperty(nameof(Checklist.CreatedAtUtc))!
            .SetValue(exactPublished, DateTime.UtcNow.AddDays(-1));

        var draftVersion = new ChecklistVersion(Guid.NewGuid(), TenantId, exactDraft.Id, 1, UserId);
        var publishedVersion = PublishedVersion(exactPublished.Id, 1);
        var genericVersion = PublishedVersion(generic.Id, 1);

        var (handler, _, _) = CreateHandler(
            new FakeChecklistRepository(exactDraft, exactPublished, generic),
            new FakeChecklistVersionRepository(draftVersion, publishedVersion, genericVersion));

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", "INTERNA_OEA"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.IsExactMatch);
        Assert.Equal(exactPublished.Id, result.ChecklistId);
    }

    [Fact]
    public async Task Resolve_ConVersionSoloDraft_NuncaLaDevuelve()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist", null, UserId);
        var draft = new ChecklistVersion(Guid.NewGuid(), TenantId, checklist.Id, 1, UserId);
        var (handler, _, _) = CreateHandler(
            new FakeChecklistRepository(checklist), new FakeChecklistVersionRepository(draft));

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResolveChecklistError.NotFound, result.Error);
    }

    [Fact]
    public async Task Resolve_SinChecklistAplicable_DevuelveNotFound()
    {
        var (handler, _, _) = CreateHandler();

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("OEA", "MAQUILA", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResolveChecklistError.NotFound, result.Error);
    }

    [Fact]
    public async Task Resolve_ProgramaInexistente_DevuelveNotFound()
    {
        var (handler, _, _) = CreateHandler();

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery("NOEXISTE", "MAQUILA", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResolveChecklistError.NotFound, result.Error);
    }

    [Fact]
    public async Task Resolve_SinProgramaOPerfil_Falla()
    {
        var (handler, _, _) = CreateHandler();

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery(null, "MAQUILA", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResolveChecklistError.ValidationFailed, result.Error);
    }

    [Fact]
    public async Task Resolve_AceptaGuidAdemasDeCodigo()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist", null, UserId);
        var version = PublishedVersion(checklist.Id, 1);
        var (handler, _, _) = CreateHandler(
            new FakeChecklistRepository(checklist), new FakeChecklistVersionRepository(version));

        var result = await handler.HandleAsync(
            new ResolveChecklistQuery(
                InMemoryProgramCatalog.Oea.Id.ToString(), InMemoryProfileCatalog.Maquila.Id.ToString(), null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(checklist.Id, result.ChecklistId);
    }
}
