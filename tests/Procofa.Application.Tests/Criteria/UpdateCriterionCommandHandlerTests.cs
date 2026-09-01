using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Criteria.UpdateCriterion;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.Criteria;

public sealed class UpdateCriterionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ChecklistId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static UpdateCriterionCommand ValidCommand(
        Guid checklistId, Guid versionId, Guid sectionId, Guid criterionId) => new(
        checklistId, versionId, sectionId, criterionId, "C-1B", "¿Actualizado?", null, null, null,
        ImportanceLevel.Media, null, null, false, 2);

    [Fact]
    public async Task UpdateCriterion_EnVersionDraft_Actualiza()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Original?", null, null, null, null, null, null, true, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new UpdateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, new FakeCriterionRepository(sections, criterion));

        var result = await handler.HandleAsync(
            ValidCommand(ChecklistId, version.Id, section.Id, criterion.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("¿Actualizado?", criterion.AuditQuestion);
        Assert.Equal(2, criterion.SortOrder);
    }

    [Fact]
    public async Task UpdateCriterion_SeccionCruzadaDeOtraVersion_DevuelveNotFound()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var otherVersion = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 2, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, otherVersion.Id, null, "Sección", null, 1);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Original?", null, null, null, null, null, null, true, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new UpdateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version, otherVersion), sections,
            new FakeCriterionRepository(sections, criterion));

        var result = await handler.HandleAsync(
            ValidCommand(ChecklistId, version.Id, section.Id, criterion.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateCriterionError.NotFound, result.Error);
    }

    [Fact]
    public async Task UpdateCriterion_VersionPublicada_DevuelveConflicto()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Original?", null, null, null, null, null, null, true, 1);
        version.Publish(DateTime.UtcNow);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new UpdateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, new FakeCriterionRepository(sections, criterion));

        var result = await handler.HandleAsync(
            ValidCommand(ChecklistId, version.Id, section.Id, criterion.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateCriterionError.VersionPublished, result.Error);
        Assert.Equal("¿Original?", criterion.AuditQuestion);
    }

    [Fact]
    public async Task UpdateCriterion_CodeYaUsadoPorOtroCriterio_Falla()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Original?", null, null, null, null, null, null, true, 1);
        var other = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1B", "¿Otro?", null, null, null, null, null, null, true, 2);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new UpdateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, new FakeCriterionRepository(sections, criterion, other));

        var result = await handler.HandleAsync(
            ValidCommand(ChecklistId, version.Id, section.Id, criterion.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateCriterionError.CodeAlreadyExists, result.Error);
    }
}
