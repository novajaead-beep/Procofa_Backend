using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Audits.ReplaceAuditPrograms;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.UseCases.Audits;

public sealed class ReplaceAuditProgramsCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static Audit CreateSeedAudit()
    {
        var audit = new Audit(
            Guid.NewGuid(), TenantId, "AUD-SEED-0001", Guid.NewGuid(), Guid.NewGuid(), null,
            InMemoryAuditTypeCatalog.InternaOea.Id, InMemoryProfileCatalog.Maquila.Id, Guid.NewGuid(), "Objetivo",
            "Alcance", null, new DateOnly(2026, 3, 1), Guid.NewGuid(), ExecutionMode.Remote);

        audit.ReplacePrograms([InMemoryProgramCatalog.Oea.Id, InMemoryProgramCatalog.Ctpat.Id]);
        return audit;
    }

    [Fact]
    public async Task ReplaceAuditPrograms_ReemplazaColeccionCompleta()
    {
        var audit = CreateSeedAudit();
        var audits = new FakeAuditRepository(audit);
        var handler = new ReplaceAuditProgramsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits, new FakeProgramRepository(),
            new FakeAuditChecklistRepository(new FakeChecklistRepository(), new FakeChecklistVersionRepository()));

        var result = await handler.HandleAsync(
            new ReplaceAuditProgramsCommand(audit.Id, [InMemoryProgramCatalog.Ctpat.Code]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var programId = Assert.Single(audit.Programs).ProgramId;
        Assert.Equal(InMemoryProgramCatalog.Ctpat.Id, programId);
    }

    [Fact]
    public async Task ReplaceAuditPrograms_AuditoriaInexistente_Falla()
    {
        var audits = new FakeAuditRepository();
        var handler = new ReplaceAuditProgramsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits, new FakeProgramRepository(),
            new FakeAuditChecklistRepository());

        var result = await handler.HandleAsync(
            new ReplaceAuditProgramsCommand(Guid.NewGuid(), [InMemoryProgramCatalog.Oea.Code]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditProgramsError.NotFound, result.Error);
    }

    [Fact]
    public async Task ReplaceAuditPrograms_QuitarProgramaSinChecklistDependiente_Ok()
    {
        var audit = CreateSeedAudit();
        var audits = new FakeAuditRepository(audit);
        var checklists = new FakeChecklistRepository();
        var versions = new FakeChecklistVersionRepository();
        var auditChecklists = new FakeAuditChecklistRepository(checklists, versions);

        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist OEA", null, Guid.NewGuid());
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, checklist.Id, 1, Guid.NewGuid());
        version.Publish(DateTime.UtcNow);
        await checklists.AddAsync(checklist, CancellationToken.None);
        await versions.CreateNextVersionAsync(TenantId, checklist.Id, _ => version, CancellationToken.None);
        await auditChecklists.ReplaceAsync(
            TenantId, audit.Id, [new AuditChecklist(Guid.NewGuid(), TenantId, audit.Id, version.Id)],
            CancellationToken.None);

        var handler = new ReplaceAuditProgramsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits, new FakeProgramRepository(),
            auditChecklists);

        // Se quita CTPAT (sin checklist dependiente) — OEA (con checklist dependiente) se conserva.
        var result = await handler.HandleAsync(
            new ReplaceAuditProgramsCommand(audit.Id, [InMemoryProgramCatalog.Oea.Code]), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ReplaceAuditPrograms_QuitarProgramaConChecklistDependiente_Falla()
    {
        var audit = CreateSeedAudit();
        var audits = new FakeAuditRepository(audit);
        var checklists = new FakeChecklistRepository();
        var versions = new FakeChecklistVersionRepository();
        var auditChecklists = new FakeAuditChecklistRepository(checklists, versions);

        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist OEA", null, Guid.NewGuid());
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, checklist.Id, 1, Guid.NewGuid());
        version.Publish(DateTime.UtcNow);
        await checklists.AddAsync(checklist, CancellationToken.None);
        await versions.CreateNextVersionAsync(TenantId, checklist.Id, _ => version, CancellationToken.None);
        await auditChecklists.ReplaceAsync(
            TenantId, audit.Id, [new AuditChecklist(Guid.NewGuid(), TenantId, audit.Id, version.Id)],
            CancellationToken.None);

        var handler = new ReplaceAuditProgramsCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits, new FakeProgramRepository(),
            auditChecklists);

        // Se quita OEA — pero el checklist ya asignado depende de OEA.
        var result = await handler.HandleAsync(
            new ReplaceAuditProgramsCommand(audit.Id, [InMemoryProgramCatalog.Ctpat.Code]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditProgramsError.ChecklistOrphaned, result.Error);
        Assert.Equal(2, audit.Programs.Count);
    }
}
