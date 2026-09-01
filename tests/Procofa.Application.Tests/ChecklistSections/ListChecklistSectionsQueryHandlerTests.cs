using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.ChecklistSections.ListChecklistSections;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.ChecklistSections;

public sealed class ListChecklistSectionsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ChecklistId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task ListSections_OrdenaPorSortOrder()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var second = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Segunda", null, 2);
        var first = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Primera", null, 1);
        var handler = new ListChecklistSectionsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), new FakeChecklistSectionRepository(second, first));

        var result = await handler.HandleAsync(
            new ListChecklistSectionsQuery(ChecklistId, version.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Primera", "Segunda"], result.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task ListSections_VersionDeOtroChecklist_Falla()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var handler = new ListChecklistSectionsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), new FakeChecklistSectionRepository());

        var result = await handler.HandleAsync(
            new ListChecklistSectionsQuery(Guid.NewGuid(), version.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ListChecklistSectionsError.VersionNotFound, result.Error);
    }
}
