using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.ChecklistVersions.GetChecklistVersion;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.ChecklistVersions;

public sealed class GetChecklistVersionQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task GetVersion_Existente_DevuelveEstadoDraft()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, Guid.NewGuid(), 1, UserId);
        var handler = new GetChecklistVersionQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version));

        var result = await handler.HandleAsync(
            new GetChecklistVersionQuery(version.ChecklistId, version.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("DRAFT", result.Status);
    }

    [Fact]
    public async Task GetVersion_DeOtroChecklist_DevuelveNotFound()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, Guid.NewGuid(), 1, UserId);
        var handler = new GetChecklistVersionQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version));

        var result = await handler.HandleAsync(
            new GetChecklistVersionQuery(Guid.NewGuid(), version.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetChecklistVersionError.NotFound, result.Error);
    }
}
