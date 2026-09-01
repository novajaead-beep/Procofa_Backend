using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Checklists.ChangeChecklistStatus;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.Checklists;

public sealed class ChangeChecklistStatusCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task ChangeChecklistStatus_Desactivar_Persiste()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist", null, UserId);
        var handler = new ChangeChecklistStatusCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(checklist));

        var result = await handler.HandleAsync(
            new ChangeChecklistStatusCommand(checklist.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(checklist.IsActive);
    }

    [Fact]
    public async Task ChangeChecklistStatus_Inexistente_Falla()
    {
        var handler = new ChangeChecklistStatusCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository());

        var result = await handler.HandleAsync(
            new ChangeChecklistStatusCommand(Guid.NewGuid(), true), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChangeChecklistStatusError.NotFound, result.Error);
    }
}
