using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.ChecklistSections.DeleteChecklistSection;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.ChecklistSections;

public sealed class DeleteChecklistSectionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ChecklistId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task DeleteSection_EnDraftSinCriterios_Elimina()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new DeleteChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), sections, new FakeCriterionRepository(sections));

        var result = await handler.HandleAsync(
            new DeleteChecklistSectionCommand(ChecklistId, version.Id, section.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(sections.Sections);
    }

    [Fact]
    public async Task DeleteSection_VersionPublicada_DevuelveConflicto()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        version.Publish(DateTime.UtcNow);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new DeleteChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), sections, new FakeCriterionRepository(sections));

        var result = await handler.HandleAsync(
            new DeleteChecklistSectionCommand(ChecklistId, version.Id, section.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteChecklistSectionError.VersionPublished, result.Error);
        Assert.Single(sections.Sections);
    }

    [Fact]
    public async Task DeleteSection_ConCriteriosAsociados_DevuelveConflicto()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Pregunta?", null, null, null, ImportanceLevel.Alta,
            null, null, true, 1);
        var handler = new DeleteChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), sections, new FakeCriterionRepository(sections, criterion));

        var result = await handler.HandleAsync(
            new DeleteChecklistSectionCommand(ChecklistId, version.Id, section.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteChecklistSectionError.HasCriteria, result.Error);
        Assert.Single(sections.Sections);
    }

    [Fact]
    public async Task DeleteSection_Inexistente_Falla()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var sections = new FakeChecklistSectionRepository();
        var handler = new DeleteChecklistSectionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version), sections, new FakeCriterionRepository(sections));

        var result = await handler.HandleAsync(
            new DeleteChecklistSectionCommand(ChecklistId, version.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteChecklistSectionError.NotFound, result.Error);
    }
}
