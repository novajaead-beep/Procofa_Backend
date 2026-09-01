using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Checklists.UpdateChecklist;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.Checklists;

public sealed class UpdateChecklistCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static Checklist NewChecklist() => new(
        Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
        "Original", null, UserId);

    private static (UpdateChecklistCommandHandler Handler, FakeChecklistRepository Checklists) CreateHandler(
        params Checklist[] seedChecklists)
    {
        var checklists = new FakeChecklistRepository(seedChecklists);
        var handler = new UpdateChecklistCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), checklists, new FakeProgramRepository(),
            new FakeProfileRepository(), new FakeAuditTypeRepository());

        return (handler, checklists);
    }

    [Fact]
    public async Task UpdateChecklist_ConDatosValidos_ActualizaElChecklist()
    {
        var checklist = NewChecklist();
        var (handler, _) = CreateHandler(checklist);
        var command = new UpdateChecklistCommand(
            checklist.Id, InMemoryProgramCatalog.Ctpat.Id, InMemoryProfileCatalog.Transportista.Id,
            InMemoryAuditTypeCatalog.InternaCtpat.Id, "Actualizado", "Nueva descripción");

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Actualizado", checklist.Name);
        Assert.Equal(InMemoryProgramCatalog.Ctpat.Id, checklist.ProgramId);
        Assert.Equal(InMemoryAuditTypeCatalog.InternaCtpat.Id, checklist.AuditTypeId);
    }

    [Fact]
    public async Task UpdateChecklist_Inexistente_Falla()
    {
        var (handler, _) = CreateHandler();
        var command = new UpdateChecklistCommand(
            Guid.NewGuid(), InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null, "X", null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateChecklistError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateChecklist_ConProgramaInexistente_Falla()
    {
        var checklist = NewChecklist();
        var (handler, _) = CreateHandler(checklist);
        var command = new UpdateChecklistCommand(
            checklist.Id, Guid.NewGuid(), InMemoryProfileCatalog.Maquila.Id, null, "X", null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateChecklistError.ProgramNotFound, result.Error);
        Assert.Equal("Original", checklist.Name);
    }

    [Fact]
    public async Task UpdateChecklist_SinName_Falla()
    {
        var checklist = NewChecklist();
        var (handler, _) = CreateHandler(checklist);
        var command = new UpdateChecklistCommand(
            checklist.Id, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null, null, null);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateChecklistError.ValidationFailed, result.Error);
    }
}
