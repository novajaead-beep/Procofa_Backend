using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Criteria.DeleteCriterion;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.Criteria;

public sealed class DeleteCriterionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ChecklistId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task DeleteCriterion_EnVersionDraft_Elimina()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Pregunta?", null, null, null, null, null, null, true, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var criteria = new FakeCriterionRepository(sections, criterion);
        var handler = new DeleteCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, criteria);

        var result = await handler.HandleAsync(
            new DeleteCriterionCommand(ChecklistId, version.Id, section.Id, criterion.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(criteria.Criteria);
    }

    [Fact]
    public async Task DeleteCriterion_SeccionCruzada_DevuelveNotFound()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var otherSection = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Otra", null, 1);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 2);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Pregunta?", null, null, null, null, null, null, true, 1);
        var sections = new FakeChecklistSectionRepository(section, otherSection);
        var criteria = new FakeCriterionRepository(sections, criterion);
        var handler = new DeleteCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, criteria);

        var result = await handler.HandleAsync(
            new DeleteCriterionCommand(ChecklistId, version.Id, otherSection.Id, criterion.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteCriterionError.NotFound, result.Error);
        Assert.Single(criteria.Criteria);
    }

    [Fact]
    public async Task DeleteCriterion_VersionPublicada_DevuelveConflicto()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Pregunta?", null, null, null, null, null, null, true, 1);
        version.Publish(DateTime.UtcNow);
        var sections = new FakeChecklistSectionRepository(section);
        var criteria = new FakeCriterionRepository(sections, criterion);
        var handler = new DeleteCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, criteria);

        var result = await handler.HandleAsync(
            new DeleteCriterionCommand(ChecklistId, version.Id, section.Id, criterion.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DeleteCriterionError.VersionPublished, result.Error);
        Assert.Single(criteria.Criteria);
    }
}
