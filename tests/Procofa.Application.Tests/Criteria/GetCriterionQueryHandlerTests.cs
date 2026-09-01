using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Criteria.GetCriterion;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.Criteria;

public sealed class GetCriterionQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid ChecklistId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    [Fact]
    public async Task GetCriterion_Existente_DevuelveDatos()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Pregunta?", null, null, null, ImportanceLevel.Baja,
            null, null, true, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new GetCriterionQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, new FakeCriterionRepository(sections, criterion));

        var result = await handler.HandleAsync(
            new GetCriterionQuery(ChecklistId, version.Id, section.Id, criterion.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("C-1", result.Code);
        Assert.Equal(ImportanceLevel.Baja, result.ImportanceLevel);
    }

    [Fact]
    public async Task GetCriterion_ChecklistCruzado_DevuelveNotFound()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, ChecklistId, 1, UserId);
        var section = new ChecklistSection(Guid.NewGuid(), TenantId, version.Id, null, "Sección", null, 1);
        var criterion = new Criterion(
            Guid.NewGuid(), TenantId, section.Id, "C-1", "¿Pregunta?", null, null, null, null, null, null, true, 1);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = new GetCriterionQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository(version),
            sections, new FakeCriterionRepository(sections, criterion));

        var result = await handler.HandleAsync(
            new GetCriterionQuery(Guid.NewGuid(), version.Id, section.Id, criterion.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetCriterionError.NotFound, result.Error);
    }
}
