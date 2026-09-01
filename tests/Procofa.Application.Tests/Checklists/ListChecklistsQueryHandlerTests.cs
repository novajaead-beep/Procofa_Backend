using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Checklists.ListChecklists;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.Checklists;

public sealed class ListChecklistsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static Checklist NewChecklist(string name, bool isActive = true)
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            name, null, UserId);

        if (!isActive)
        {
            checklist.Deactivate();
        }

        return checklist;
    }

    [Fact]
    public async Task ListChecklists_FiltraPorIsActive()
    {
        var active = NewChecklist("Activo");
        var inactive = NewChecklist("Inactivo", isActive: false);
        var handler = new ListChecklistsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistRepository(active, inactive));

        var result = await handler.HandleAsync(
            new ListChecklistsQuery(null, null, null, null, true, 1, 25), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(active.Id, item.Id);
    }

    [Fact]
    public async Task ListChecklists_AplicaPaginacionConDefaults()
    {
        var checklists = Enumerable.Range(1, 3).Select(i => NewChecklist($"Checklist {i}")).ToArray();
        var handler = new ListChecklistsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(checklists));

        var result = await handler.HandleAsync(
            new ListChecklistsQuery(null, null, null, null, null, 0, 0), CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(3, result.Total);
    }

    [Fact]
    public async Task ListChecklists_PageSizeSuperiorAlMaximo_SeLimitaA100()
    {
        var handler = new ListChecklistsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository());

        var result = await handler.HandleAsync(
            new ListChecklistsQuery(null, null, null, null, null, 1, 500), CancellationToken.None);

        Assert.Equal(100, result.PageSize);
    }
}
