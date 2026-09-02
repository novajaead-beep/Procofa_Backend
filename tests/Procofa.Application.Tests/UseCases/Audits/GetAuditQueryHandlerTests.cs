using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Audits.GetAudit;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.UseCases.Audits;

public sealed class GetAuditQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static Audit CreateSeedAudit(Guid clientId) => new(
        Guid.NewGuid(), TenantId, "AUD-SEED-0004", clientId, Guid.NewGuid(), null,
        InMemoryAuditTypeCatalog.InternaOea.Id, InMemoryProfileCatalog.Maquila.Id, Guid.NewGuid(), "Objetivo",
        "Alcance", null, new DateOnly(2026, 3, 1), Guid.NewGuid(), ExecutionMode.Remote);

    [Fact]
    public async Task GetAudit_Existente_DevuelveDetalle()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var audit = CreateSeedAudit(client.Id);
        audit.ReplacePrograms([InMemoryProgramCatalog.Oea.Id]);

        var handler = new GetAuditQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(audit),
            new FakeProgramRepository(), new FakeUserRepository(),
            new FakeAuditChecklistRepository(new FakeChecklistRepository(), new FakeChecklistVersionRepository()),
            new FakeCurrentUser(Guid.NewGuid(), "ADMIN"));

        var result = await handler.HandleAsync(new GetAuditQuery(audit.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(audit.Folio, result.Folio);
        Assert.Contains(InMemoryProgramCatalog.Oea.Code, result.ProgramCodes);
        Assert.Empty(result.Checklists);
    }

    [Fact]
    public async Task GetAudit_ConChecklistAsignado_LoDevuelveEnElDetalle()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var audit = CreateSeedAudit(client.Id);
        audit.ReplacePrograms([InMemoryProgramCatalog.Oea.Id]);

        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, InMemoryProfileCatalog.Maquila.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, "Checklist Asignado", null, Guid.NewGuid());
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, checklist.Id, 1, Guid.NewGuid());
        version.Publish(DateTime.UtcNow);
        var checklists = new FakeChecklistRepository(checklist);
        var versions = new FakeChecklistVersionRepository(version);
        var auditChecklists = new FakeAuditChecklistRepository(checklists, versions);
        await auditChecklists.ReplaceAsync(
            TenantId, audit.Id, [new AuditChecklist(Guid.NewGuid(), TenantId, audit.Id, version.Id)],
            CancellationToken.None);

        var handler = new GetAuditQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(audit),
            new FakeProgramRepository(), new FakeUserRepository(), auditChecklists,
            new FakeCurrentUser(Guid.NewGuid(), "ADMIN"));

        var result = await handler.HandleAsync(new GetAuditQuery(audit.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Checklists);
        Assert.Equal(checklist.Id, item.ChecklistId);
        Assert.Equal(version.Id, item.ChecklistVersionId);
        Assert.Equal(checklist.Name, item.ChecklistName);
    }

    [Fact]
    public async Task GetAudit_Inexistente_DevuelveNotFound()
    {
        var handler = new GetAuditQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(),
            new FakeProgramRepository(), new FakeUserRepository(), new FakeAuditChecklistRepository(),
            new FakeCurrentUser(Guid.NewGuid(), "ADMIN"));

        var result = await handler.HandleAsync(new GetAuditQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetAuditError.NotFound, result.Error);
    }

    [Fact]
    public async Task GetAudit_ClienteFueraDeAlcance_DevuelveNotFound()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var audit = CreateSeedAudit(client.Id);

        var clienteUserId = Guid.NewGuid();
        var clienteUser = new User(
            clienteUserId, TenantId, "cliente.usuario@procofa-test.invalid", "hash", "Nombre", "Apellido", null);

        var handler = new GetAuditQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), new FakeAuditRepository(audit),
            new FakeProgramRepository(), new FakeUserRepository(clienteUser), new FakeAuditChecklistRepository(),
            new FakeCurrentUser(clienteUserId, "CLIENTE"));

        var result = await handler.HandleAsync(new GetAuditQuery(audit.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetAuditError.NotFound, result.Error);
    }
}
