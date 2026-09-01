using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.ChecklistSections.CreateChecklistSection;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.ChecklistSections;

public sealed class CreateChecklistSectionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ChecklistId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static ChecklistVersion DraftVersion() =>
        new(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);

    [Fact]
    public async Task CreateSection_EnVersionDraft_Crea()
    {
        var version = DraftVersion();
        var sections = new FakeChecklistSectionRepository();
        var handler = new CreateChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), sections);

        var result = await handler.HandleAsync(
            new CreateChecklistSectionCommand(ChecklistId, version.Id, "SEC-1", "Sección 1", null, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(sections.Sections);
    }

    [Fact]
    public async Task CreateSection_VersionDeOtroChecklist_DevuelveNotFound()
    {
        var version = DraftVersion();
        var sections = new FakeChecklistSectionRepository();
        var handler = new CreateChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), sections);

        var result = await handler.HandleAsync(
            new CreateChecklistSectionCommand(Guid.NewGuid(), version.Id, null, "Sección 1", null, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateChecklistSectionError.VersionNotFound, result.Error);
        Assert.Empty(sections.Sections);
    }

    [Fact]
    public async Task CreateSection_VersionPublicada_DevuelveConflicto()
    {
        var version = DraftVersion();
        version.Publish(DateTime.UtcNow);
        var sections = new FakeChecklistSectionRepository();
        var handler = new CreateChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), sections);

        var result = await handler.HandleAsync(
            new CreateChecklistSectionCommand(ChecklistId, version.Id, null, "Sección 1", null, 1),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateChecklistSectionError.VersionPublished, result.Error);
    }

    [Fact]
    public async Task CreateSection_SinName_Falla()
    {
        var version = DraftVersion();
        var sections = new FakeChecklistSectionRepository();
        var handler = new CreateChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), sections);

        var result = await handler.HandleAsync(
            new CreateChecklistSectionCommand(ChecklistId, version.Id, null, null, null, 1), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateChecklistSectionError.ValidationFailed, result.Error);
    }
}
