using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Criteria.ListCriteria;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.Criteria;

public sealed class ListCriteriaQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ChecklistId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task ListCriteria_DeLaSeccionCorrecta_DevuelveOrdenadosPorSortOrder()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var second = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-2", "¿Segunda?", null, null, null, null, null, null, true, 2);
        var first = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Primera?", null, null, null, null, null, null, true, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new ListCriteriaQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, new FakeCriterionRepository(sections, second, first));

        var result = await handler.HandleAsync(
            new ListCriteriaQuery(ChecklistId, version.Id, section.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["C-1", "C-2"], result.Items.Select(i => i.Code));
    }

    /// <summary>Regresión: el handler debe validar checklistId→versión ANTES de resolver la
    /// sección — un versionId y sectionId reales de otro checklist no deben devolver datos.</summary>
    [Fact]
    public async Task ListCriteria_ChecklistIdCruzado_DevuelveNotFound()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Pregunta?", null, null, null, null, null, null, true, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new ListCriteriaQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, new FakeCriterionRepository(sections, criterion));

        var result = await handler.HandleAsync(
            new ListCriteriaQuery(Guid.NewGuid(), version.Id, section.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ListCriteriaError.SectionNotFound, result.Error);
    }

    [Fact]
    public async Task ListCriteria_SeccionDeOtraVersion_DevuelveNotFound()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var otherVersion = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 2, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, otherVersion.Id, null, "Sección", null, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new ListCriteriaQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version, otherVersion), sections,
            new FakeCriterionRepository(sections));

        var result = await handler.HandleAsync(
            new ListCriteriaQuery(ChecklistId, version.Id, section.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ListCriteriaError.SectionNotFound, result.Error);
    }
}
