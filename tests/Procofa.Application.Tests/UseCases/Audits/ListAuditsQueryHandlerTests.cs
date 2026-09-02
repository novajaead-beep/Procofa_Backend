using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Audits.ListAudits;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.UseCases.Audits;

public sealed class ListAuditsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static Audit CreateAudit(Guid clientId, string folio) => new(
        Guid.NewGuid(), TenantId, folio, clientId, Guid.NewGuid(), null, InMemoryAuditTypeCatalog.InternaOea.Id,
        InMemoryProfileCatalog.Maquila.Id, Guid.NewGuid(), "Objetivo", "Alcance", null, new DateOnly(2026, 3, 1),
        Guid.NewGuid(), ExecutionMode.Remote);

    [Fact]
    public async Task ListAudits_Admin_VeTodoElTenant()
    {
        var clientA = Guid.NewGuid();
        var clientB = Guid.NewGuid();
        var audits = new FakeAuditRepository(
            CreateAudit(clientA, "AUD-A"), CreateAudit(clientB, "AUD-B"));

        var handler = new ListAuditsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits, new FakeUserRepository(),
            new FakeCurrentUser(Guid.NewGuid(), "ADMIN"));

        var result = await handler.HandleAsync(
            new ListAuditsQuery(null, null, null, null, null, null, 1, 25), CancellationToken.None);

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public async Task ListAudits_ExecutionModeInvalido_Falla()
    {
        var audits = new FakeAuditRepository(CreateAudit(Guid.NewGuid(), "AUD-A"));

        var handler = new ListAuditsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits, new FakeUserRepository(),
            new FakeCurrentUser(Guid.NewGuid(), "ADMIN"));

        var result = await handler.HandleAsync(
            new ListAuditsQuery(null, null, null, null, "INVALIDO", null, 1, 25), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ListAuditsError.ValidationFailed, result.Error);
    }

    [Fact]
    public async Task ListAudits_ClienteSinAcceso_DevuelveListaVacia()
    {
        var clienteUserId = Guid.NewGuid();
        var clienteUser = new User(
            clienteUserId, TenantId, "cliente@procofa-test.invalid", "hash", "Nombre", "Apellido", null);

        var audits = new FakeAuditRepository(CreateAudit(Guid.NewGuid(), "AUD-A"));

        var handler = new ListAuditsQueryHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits,
            new FakeUserRepository(clienteUser), new FakeCurrentUser(clienteUserId, "CLIENTE"));

        var result = await handler.HandleAsync(
            new ListAuditsQuery(null, null, null, null, null, null, 1, 25), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
    }
}
