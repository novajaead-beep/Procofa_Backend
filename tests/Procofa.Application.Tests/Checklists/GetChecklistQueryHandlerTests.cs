using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Checklists.GetChecklist;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.Checklists;

public sealed class GetChecklistQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenantId = Guid.Parse("00000000-0000-0000-0000-000000000099");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task GetChecklist_Existente_DevuelveDatos()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist", "Desc", UserId);
        var handler = new GetChecklistQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(checklist));

        var result = await handler.HandleAsync(new GetChecklistQuery(checklist.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(checklist.Id, result.Id);
        Assert.Equal("Checklist", result.Name);
    }

    [Fact]
    public async Task GetChecklist_Inexistente_DevuelveNotFound()
    {
        var handler = new GetChecklistQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository());

        var result = await handler.HandleAsync(new GetChecklistQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetChecklistError.NotFound, result.Error);
    }

    [Fact]
    public async Task GetChecklist_DeOtroTenant_DevuelveNotFound()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), OtherTenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist", null, UserId);
        var handler = new GetChecklistQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(checklist));

        var result = await handler.HandleAsync(new GetChecklistQuery(checklist.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
