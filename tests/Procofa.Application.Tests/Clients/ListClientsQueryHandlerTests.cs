using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Clients.ListClients;
using Procofa.Application.UseCases.Users;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Tests.Clients;

public sealed class ListClientsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static ListClientsQueryHandler CreateHandler(
        FakeClientRepository clients, FakeAuditedCompanyRepository? companies = null,
        FakeUserRepository? users = null, FakeCurrentUser? currentUser = null) =>
        new(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), clients,
            companies ?? new FakeAuditedCompanyRepository(), users ?? new FakeUserRepository(),
            currentUser ?? new FakeCurrentUser(Guid.NewGuid(), UserRoleCodes.Admin));

    [Fact]
    public async Task ListClients_FiltraPorSearch()
    {
        var uno = new Client(Guid.NewGuid(), TenantId, "Manufacturas Del Norte", null, null, null, null, null);
        var dos = new Client(Guid.NewGuid(), TenantId, "Otro Distinto", null, null, null, null, null);
        var clients = new FakeClientRepository(uno, dos);
        var handler = CreateHandler(clients);

        var result = await handler.HandleAsync(
            new ListClientsQuery("Norte", null, null, 1, 25), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(uno.Id, item.Id);
    }

    [Fact]
    public async Task ListClients_FiltraPorIsActive()
    {
        var activo = new Client(Guid.NewGuid(), TenantId, "Activo", null, null, null, null, null);
        var inactivo = new Client(Guid.NewGuid(), TenantId, "Inactivo", null, null, null, null, null);
        inactivo.Deactivate();
        var clients = new FakeClientRepository(activo, inactivo);
        var handler = CreateHandler(clients);

        var result = await handler.HandleAsync(
            new ListClientsQuery(null, true, null, 1, 25), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(activo.Id, item.Id);
    }

    [Fact]
    public async Task ListClients_IncluyeCantidadDeEmpresasAuditadas()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var company = new AuditedCompany(Guid.NewGuid(), TenantId, client.Id, null, "Empresa", null, null, null, null, false);
        var clients = new FakeClientRepository(client);
        var companies = new FakeAuditedCompanyRepository(company);
        var handler = CreateHandler(clients, companies);

        var result = await handler.HandleAsync(new ListClientsQuery(null, null, null, 1, 25), CancellationToken.None);

        Assert.Equal(1, Assert.Single(result.Items).AuditedCompanyCount);
    }

    [Fact]
    public async Task ListClients_RolCliente_SoloVeLosClientsAsignados()
    {
        var visible = new Client(Guid.NewGuid(), TenantId, "Visible", null, null, null, null, null);
        var oculto = new Client(Guid.NewGuid(), TenantId, "Oculto", null, null, null, null, null);
        var clients = new FakeClientRepository(visible, oculto);

        var clienteUser = new User(Guid.NewGuid(), TenantId, "cliente@procofa.com", "hash", "C", "L", null);
        clienteUser.GrantClientAccess(new Domain.Entities.Identity.ValueObjects.UserClientAccess(
            TenantId, clienteUser.Id, visible.Id, grantedByUserId: null));
        var users = new FakeUserRepository(clienteUser);

        var handler = CreateHandler(
            clients, users: users, currentUser: new FakeCurrentUser(clienteUser.Id, UserRoleCodes.Cliente));

        var result = await handler.HandleAsync(new ListClientsQuery(null, null, null, 1, 25), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(visible.Id, item.Id);
    }

    [Fact]
    public async Task ListClients_RolClienteSinAccesos_NoVeNingunClient()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var clients = new FakeClientRepository(client);

        var clienteUser = new User(Guid.NewGuid(), TenantId, "sin-acceso@procofa.com", "hash", "C", "L", null);
        var users = new FakeUserRepository(clienteUser);

        var handler = CreateHandler(
            clients, users: users, currentUser: new FakeCurrentUser(clienteUser.Id, UserRoleCodes.Cliente));

        var result = await handler.HandleAsync(new ListClientsQuery(null, null, null, 1, 25), CancellationToken.None);

        Assert.Empty(result.Items);
    }
}
