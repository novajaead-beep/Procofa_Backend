using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.ChecklistVersions.CreateChecklistVersion;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.ChecklistVersions;

public sealed class CreateChecklistVersionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static Checklist NewChecklist() => new(
        Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id, null,
        "Checklist", null, UserId);

    [Fact]
    public async Task CreateVersion_Primera_AsignaNumero1()
    {
        var checklist = NewChecklist();
        var handler = new CreateChecklistVersionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(checklist),
            new FakeChecklistVersionRepository(), new FakeCurrentUser(UserId, "ADMIN"));

        var result = await handler.HandleAsync(
            new CreateChecklistVersionCommand(checklist.Id, "Primera versión"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.VersionNumber);
    }

    [Fact]
    public async Task CreateVersion_ConVersionesPrevias_IncrementaSecuencialmente()
    {
        var checklist = NewChecklist();
        var existing = new ChecklistVersion(Guid.NewGuid(), TenantId, checklist.Id, 1, UserId);
        var handler = new CreateChecklistVersionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(checklist),
            new FakeChecklistVersionRepository(existing), new FakeCurrentUser(UserId, "ADMIN"));

        var result = await handler.HandleAsync(
            new CreateChecklistVersionCommand(checklist.Id, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.VersionNumber);
    }

    [Fact]
    public async Task CreateVersion_SinChangeNotes_QuedaVaciaSinClonarContenido()
    {
        var checklist = NewChecklist();
        var sections = new FakeChecklistSectionRepository();
        var handler = new CreateChecklistVersionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(checklist),
            new FakeChecklistVersionRepository(), new FakeCurrentUser(UserId, "ADMIN"));

        var result = await handler.HandleAsync(
            new CreateChecklistVersionCommand(checklist.Id, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(await sections.ListByVersionAsync(TenantId, result.VersionId!.Value, CancellationToken.None));
    }

    [Fact]
    public async Task CreateVersion_ChecklistInexistente_Falla()
    {
        var handler = new CreateChecklistVersionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistRepository(),
            new FakeChecklistVersionRepository(), new FakeCurrentUser(UserId, "ADMIN"));

        var result = await handler.HandleAsync(
            new CreateChecklistVersionCommand(Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateChecklistVersionError.ChecklistNotFound, result.Error);
    }
}
