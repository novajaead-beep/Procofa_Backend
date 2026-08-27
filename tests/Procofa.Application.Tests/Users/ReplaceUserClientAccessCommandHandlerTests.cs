using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Users.ReplaceUserClientAccess;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.Tests.Users;

/// <summary>Tests de <see cref="ReplaceUserClientAccessCommandHandler"/> (Instrucción 05, sección "ACCESO A CLIENTES").</summary>
public sealed class ReplaceUserClientAccessCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private static User CreateClienteUser()
    {
        var user = new User(Guid.NewGuid(), TenantId, "cliente@procofa.com", "hash", "Ana", "Cliente", phone: null);
        user.AddRole(new UserRole(TenantId, user.Id, InMemoryRoleCatalog.Cliente.Id, assignedByUserId: null));
        return user;
    }

    private static ReplaceUserClientAccessCommandHandler CreateHandler(
        FakeUserRepository users, FakeClientRepository clients) =>
        new(
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            users,
            new FakeRoleRepository(),
            clients,
            new FakeCurrentUser(Guid.NewGuid()));

    [Fact]
    public async Task ReplaceClientAccess_UnUsuarioCliente_RecibeAcceso()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente S.A.", null, null, null, null, null);
        var user = CreateClienteUser();
        var handler = CreateHandler(new FakeUserRepository(user), new FakeClientRepository(client));

        var result = await handler.HandleAsync(
            new ReplaceUserClientAccessCommand(user.Id, [client.Id]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(user.ClientAccess, a => a.ClientId == client.Id);
    }

    [Fact]
    public async Task ReplaceClientAccess_ReemplazaLosAccesosAnteriores()
    {
        var oldClient = new Client(Guid.NewGuid(), TenantId, "Cliente Anterior", null, null, null, null, null);
        var newClient = new Client(Guid.NewGuid(), TenantId, "Cliente Nuevo", null, null, null, null, null);
        var user = CreateClienteUser();
        user.GrantClientAccess(new UserClientAccess(TenantId, user.Id, oldClient.Id, grantedByUserId: null));

        var handler = CreateHandler(
            new FakeUserRepository(user), new FakeClientRepository(oldClient, newClient));

        var result = await handler.HandleAsync(
            new ReplaceUserClientAccessCommand(user.Id, [newClient.Id]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var clientIds = user.ClientAccess.Select(a => a.ClientId).ToArray();
        Assert.DoesNotContain(oldClient.Id, clientIds);
        Assert.Contains(newClient.Id, clientIds);
    }

    [Fact]
    public async Task ReplaceClientAccess_UsuarioSinRolCliente_DevuelveConflicto()
    {
        var user = new User(Guid.NewGuid(), TenantId, "auditor@procofa.com", "hash", "Ana", "Auditora", phone: null);
        user.AddRole(new UserRole(TenantId, user.Id, InMemoryRoleCatalog.AuditorApoyo.Id, assignedByUserId: null));
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente S.A.", null, null, null, null, null);

        var handler = CreateHandler(new FakeUserRepository(user), new FakeClientRepository(client));

        var result = await handler.HandleAsync(
            new ReplaceUserClientAccessCommand(user.Id, [client.Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceUserClientAccessError.UserNotCliente, result.Error);
    }

    [Fact]
    public async Task ReplaceClientAccess_ConClientDeOtroTenant_NoEsAceptado()
    {
        var clientDeOtroTenant = new Client(Guid.NewGuid(), OtherTenantId, "Cliente Ajeno", null, null, null, null, null);
        var user = CreateClienteUser();

        var handler = CreateHandler(
            new FakeUserRepository(user), new FakeClientRepository(clientDeOtroTenant));

        var result = await handler.HandleAsync(
            new ReplaceUserClientAccessCommand(user.Id, [clientDeOtroTenant.Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceUserClientAccessError.ClientNotFound, result.Error);
        Assert.Empty(user.ClientAccess); // nunca se concedió acceso a un cliente de otro tenant.
    }
}
