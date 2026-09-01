using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Criteria.CreateCriterion;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.Criteria;

public sealed class CreateCriterionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ChecklistId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static CreateCriterionCommand ValidCommand(Guid checklistId, Guid versionId, Guid sectionId) => new(
        checklistId, versionId, sectionId, "C-1", "¿Cumple?", null, null, null, ImportanceLevel.Alta, null, null,
        true, 1);

    [Fact]
    public async Task CreateCriterion_EnSeccionDeVersionDraft_Crea()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var criteria = new FakeCriterionRepository(sections);
        var handler = new CreateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, criteria);

        var result = await handler.HandleAsync(
            ValidCommand(ChecklistId, version.Id, section.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(criteria.Criteria);
    }

    [Fact]
    public async Task CreateCriterion_SeccionDeOtraVersion_DevuelveNotFound()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var otherVersion = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 2, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, otherVersion.Id, null, "Sección", null, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var criteria = new FakeCriterionRepository(sections);
        var handler = new CreateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version, otherVersion), sections, criteria);

        var result = await handler.HandleAsync(
            ValidCommand(ChecklistId, version.Id, section.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCriterionError.SectionNotFound, result.Error);
        Assert.Empty(criteria.Criteria);
    }

    [Fact]
    public async Task CreateCriterion_VersionDeOtroChecklist_DevuelveNotFound()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var criteria = new FakeCriterionRepository(sections);
        var handler = new CreateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, criteria);

        var result = await handler.HandleAsync(
            ValidCommand(Guid.NewGuid(), version.Id, section.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCriterionError.SectionNotFound, result.Error);
    }

    [Fact]
    public async Task CreateCriterion_VersionPublicada_DevuelveConflicto()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        version.Publish(DateTime.UtcNow);
        var sections = new FakeChecklistSectionRepository(section);
        var criteria = new FakeCriterionRepository(sections);
        var handler = new CreateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, criteria);

        var result = await handler.HandleAsync(
            ValidCommand(ChecklistId, version.Id, section.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCriterionError.VersionPublished, result.Error);
    }

    [Fact]
    public async Task CreateCriterion_CodeDuplicadoEnSeccion_Falla()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var existing = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Existente?", null, null, null, null, null, null, true, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var criteria = new FakeCriterionRepository(sections, existing);
        var handler = new CreateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, criteria);

        var result = await handler.HandleAsync(
            ValidCommand(ChecklistId, version.Id, section.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCriterionError.CodeAlreadyExists, result.Error);
    }

    [Fact]
    public async Task CreateCriterion_SinCodeOPregunta_Falla()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new CreateCriterionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, new FakeCriterionRepository(sections));

        var result = await handler.HandleAsync(
            ValidCommand(ChecklistId, version.Id, section.Id) with { Code = null }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCriterionError.ValidationFailed, result.Error);
    }
}
