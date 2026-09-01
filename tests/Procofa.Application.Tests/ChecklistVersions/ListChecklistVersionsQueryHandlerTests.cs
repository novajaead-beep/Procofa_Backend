using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.ChecklistVersions.ListChecklistVersions;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.ChecklistVersions;

public sealed class ListChecklistVersionsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task ListVersions_OrdenaPorNumeroDescendente()
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
            "Checklist", null, UserId);
        var v1 = new ChecklistVersion(Guid.NewGuid(), TenantId, checklist.Id, 1, UserId);
        var v2 = new ChecklistVersion(Guid.NewGuid(), TenantId, checklist.Id, 2, UserId);
        var handler = new ListChecklistVersionsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(checklist),
            new FakeChecklistVersionRepository(v1, v2));

        var result = await handler.HandleAsync(
            new ListChecklistVersionsQuery(checklist.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([2, 1], result.Items.Select(i => i.VersionNumber));
    }

    [Fact]
    public async Task ListVersions_ChecklistInexistente_Falla()
    {
        var handler = new ListChecklistVersionsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(),
            new FakeChecklistVersionRepository());

        var result = await handler.HandleAsync(
            new ListChecklistVersionsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ListChecklistVersionsError.ChecklistNotFound, result.Error);
    }
}
