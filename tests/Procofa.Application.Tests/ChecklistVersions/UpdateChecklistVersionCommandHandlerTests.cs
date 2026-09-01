using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.ChecklistVersions.UpdateChecklistVersion;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Tests.ChecklistVersions;

public sealed class UpdateChecklistVersionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task UpdateVersion_EnDraft_Actualiza()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, Guid.NewGuid(), 1, UserId);
        var handler = new UpdateChecklistVersionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version));

        var result = await handler.HandleAsync(
            new UpdateChecklistVersionCommand(version.ChecklistId, version.Id, "Notas"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Notas", version.ChangeNotes);
    }

    [Fact]
    public async Task UpdateVersion_Publicada_DevuelveConflicto()
    {
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, Guid.NewGuid(), 1, UserId);
        version.Publish(DateTime.UtcNow);
        var handler = new UpdateChecklistVersionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(),
            new FakeChecklistVersionRepository(version));

        var result = await handler.HandleAsync(
            new UpdateChecklistVersionCommand(version.ChecklistId, version.Id, "Notas"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateChecklistVersionError.VersionPublished, result.Error);
    }

    [Fact]
    public async Task UpdateVersion_Inexistente_Falla()
    {
        var handler = new UpdateChecklistVersionCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeChecklistVersionRepository());

        var result = await handler.HandleAsync(
            new UpdateChecklistVersionCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateChecklistVersionError.NotFound, result.Error);
    }
}
