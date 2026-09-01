using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Checklists.CreateChecklist;

namespace Procofa.Application.Tests.Checklists;

public sealed class CreateChecklistCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static (CreateChecklistCommandHandler Handler, FakeChecklistRepository Checklists) CreateHandler(
        FakeChecklistRepository? checklists = null)
    {
        checklists ??= new FakeChecklistRepository();
        var handler = new CreateChecklistCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), checklists, new FakeProgramRepository(),
            new FakeProfileRepository(), new FakeAuditTypeRepository(), new FakeCurrentUser(UserId, "ADMIN"));

        return (handler, checklists);
    }

    private static CreateChecklistCommand ValidCommand(Guid? auditTypeId = null) => new(
        InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, auditTypeId, "Checklist OEA Maquila",
        "Descripción");

    [Fact]
    public async Task CreateChecklist_ConDatosValidos_CreaElChecklist()
    {
        var (handler, checklists) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(checklists.Checklists);
        Assert.Equal(result.ChecklistId, created.Id);
        Assert.Equal(TenantId, created.TenantId);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task CreateChecklist_ConAuditTypeNull_QuedaGenerico()
    {
        var (handler, checklists) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand(auditTypeId: null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(checklists.Checklists);
        Assert.Null(created.AuditTypeId);
    }

    [Fact]
    public async Task CreateChecklist_ConProgramaInexistente_Falla()
    {
        var (handler, checklists) = CreateHandler();
        var command = ValidCommand() with { ProgramId = Guid.NewGuid() };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateChecklistError.ProgramNotFound, result.Error);
        Assert.Empty(checklists.Checklists);
    }

    [Fact]
    public async Task CreateChecklist_ConPerfilInexistente_Falla()
    {
        var (handler, checklists) = CreateHandler();
        var command = ValidCommand() with { ProfileId = Guid.NewGuid() };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateChecklistError.ProfileNotFound, result.Error);
        Assert.Empty(checklists.Checklists);
    }

    [Fact]
    public async Task CreateChecklist_ConAuditTypeInexistente_Falla()
    {
        var (handler, checklists) = CreateHandler();
        var command = ValidCommand(Guid.NewGuid());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateChecklistError.AuditTypeNotFound, result.Error);
        Assert.Empty(checklists.Checklists);
    }

    [Fact]
    public async Task CreateChecklist_SinName_Falla()
    {
        var (handler, checklists) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand() with { Name = null }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateChecklistError.ValidationFailed, result.Error);
        Assert.Empty(checklists.Checklists);
    }

    [Fact]
    public async Task CreateChecklist_MismaCombinacionProgramaPerfilTipo_NoValidaUnicidad()
    {
        var (handler, checklists) = CreateHandler();

        var first = await handler.HandleAsync(ValidCommand(), CancellationToken.None);
        var second = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, checklists.Checklists.Count);
    }
}
