using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.ChecklistVersions.PublishChecklistVersion;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.ChecklistVersions;

public sealed class PublishChecklistVersionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly DateTime FixedUtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static ChecklistSection NewSection(Guid versionId) =>
        new(Guid.NewGuid(), TenantId, versionId, "SEC-1", "Sección", null, 1);

    private static Criterion NewCriterion(Guid sectionId) =>
        new(Guid.NewGuid(), TenantId, sectionId, "CRIT-1", "¿Pregunta?", null, null, null, ImportanceLevel.Alta,
            null, null, true, 1);

    private static PublishChecklistVersionCommandHandler CreateHandler(
        FakeChecklistVersionRepository versions, FakeChecklistSectionRepository sections,
        FakeCriterionRepository criteria) =>
        new(new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), versions, sections, criteria,
            new FakeDateTimeProvider(FixedUtcNow));

    [Fact]
    public async Task Publish_ConSeccionesYCriterios_Publica()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, Guid.NewGuid(), 1, UserId);
        var section = NewSection(version.Id);
        var sections = new FakeChecklistSectionRepository(section);
        var criteria = new FakeCriterionRepository(sections, NewCriterion(section.Id));
        var handler = CreateHandler(new FakeChecklistVersionRepository(version), sections, criteria);

        var result = await handler.HandleAsync(
            new PublishChecklistVersionCommand(version.ChecklistId, version.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ChecklistVersionStatus.Published, version.Status);
        Assert.Equal(FixedUtcNow, version.PublishedAtUtc);
    }

    [Fact]
    public async Task Publish_YaPublicada_DevuelveConflicto()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, Guid.NewGuid(), 1, UserId);
        version.Publish(DateTime.UtcNow);
        var sections = new FakeChecklistSectionRepository();
        var handler = CreateHandler(
            new FakeChecklistVersionRepository(version), sections, new FakeCriterionRepository(sections));

        var result = await handler.HandleAsync(
            new PublishChecklistVersionCommand(version.ChecklistId, version.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PublishChecklistVersionError.AlreadyPublished, result.Error);
    }

    [Fact]
    public async Task Publish_SinSecciones_Falla()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, Guid.NewGuid(), 1, UserId);
        var sections = new FakeChecklistSectionRepository();
        var handler = CreateHandler(
            new FakeChecklistVersionRepository(version), sections, new FakeCriterionRepository(sections));

        var result = await handler.HandleAsync(
            new PublishChecklistVersionCommand(version.ChecklistId, version.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PublishChecklistVersionError.NoSections, result.Error);
    }

    [Fact]
    public async Task Publish_ConSeccionSinCriterios_Falla()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, Guid.NewGuid(), 1, UserId);
        var section = NewSection(version.Id);
        var sections = new FakeChecklistSectionRepository(section);
        var handler = CreateHandler(
            new FakeChecklistVersionRepository(version), sections, new FakeCriterionRepository(sections));

        var result = await handler.HandleAsync(
            new PublishChecklistVersionCommand(version.ChecklistId, version.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PublishChecklistVersionError.NoCriteria, result.Error);
    }

    [Fact]
    public async Task Publish_Inexistente_Falla()
    {
        var sections = new FakeChecklistSectionRepository();
        var handler = CreateHandler(
            new FakeChecklistVersionRepository(), sections, new FakeCriterionRepository(sections));

        var result = await handler.HandleAsync(
            new PublishChecklistVersionCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PublishChecklistVersionError.NotFound, result.Error);
    }
}
