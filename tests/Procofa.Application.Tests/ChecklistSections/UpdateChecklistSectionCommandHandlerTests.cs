using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.ChecklistSections.UpdateChecklistSection;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.ChecklistSections;

public sealed class UpdateChecklistSectionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ChecklistId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task UpdateSection_EnDraft_ActualizaYReordena()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, "SEC-1", "Original", null, 1);
        var handler = new UpdateChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), new FakeChecklistSectionRepository(section));

        var result = await handler.HandleAsync(
            new UpdateChecklistSectionCommand(ChecklistId, version.Id, section.Id, "SEC-1B", "Actualizada", null, 5),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Actualizada", section.Name);
        Assert.Equal(5, section.SortOrder);
    }

    [Fact]
    public async Task UpdateSection_VersionDeOtroChecklist_DevuelveNotFound()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Original", null, 1);
        var handler = new UpdateChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), new FakeChecklistSectionRepository(section));

        var result = await handler.HandleAsync(
            new UpdateChecklistSectionCommand(Guid.NewGuid(), version.Id, section.Id, null, "X", null, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateChecklistSectionError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateSection_VersionPublicada_DevuelveConflicto()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Original", null, 1);
        version.Publish(DateTime.UtcNow);
        var handler = new UpdateChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), new FakeChecklistSectionRepository(section));

        var result = await handler.HandleAsync(
            new UpdateChecklistSectionCommand(ChecklistId, version.Id, section.Id, null, "X", null, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateChecklistSectionError.VersionPublished, result.Error);
        Assert.Equal("Original", section.Name);
    }
}
