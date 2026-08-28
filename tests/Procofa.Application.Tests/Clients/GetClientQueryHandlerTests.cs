using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Clients.GetClient;
using Procofa.Application.UseCases.Users;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.Tests.Clients;

public sealed class GetClientQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static GetClientQueryHandler CreateHandler(
        FakeClientRepository clients, FakeUserRepository? users = null, FakeCurrentUser? currentUser = null) =>
        new(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), clients, new FakeAuditedCompanyRepository(),
            new FakeProgramRepository(), users ?? new FakeUserRepository(),
            currentUser ?? new FakeCurrentUser(Guid.NewGuid(), UserRoleCodes.Admin));

    [Fact]
    public async Task GetClient_Existente_DevuelveDetalle()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var handler = CreateHandler(new FakeClientRepository(client));

        var result = await handler.HandleAsync(new GetClientQuery(client.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(client.Id, result.Id);
    }

    [Fact]
    public async Task GetClient_RolCliente_ConClientNoAsignado_Devuelve404()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var clienteUser = new User(Guid.NewGuid(), TenantId, "sin-acceso@procofa.com", "hash", "C", "L", null);
        var handler = CreateHandler(
            new FakeClientRepository(client), new FakeUserRepository(clienteUser),
            new FakeCurrentUser(clienteUser.Id, UserRoleCodes.Cliente));

        var result = await handler.HandleAsync(new GetClientQuery(client.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetClientError.NotFound, result.Error);
    }

    [Fact]
    public async Task GetClient_RolCliente_ConClientAsignado_DevuelveDetalle()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var clienteUser = new User(Guid.NewGuid(), TenantId, "con-acceso@procofa.com", "hash", "C", "L", null);
        clienteUser.GrantClientAccess(new UserClientAccess(TenantId, clienteUser.Id, client.Id, grantedByUserId: null));

        var handler = CreateHandler(
            new FakeClientRepository(client), new FakeUserRepository(clienteUser),
            new FakeCurrentUser(clienteUser.Id, UserRoleCodes.Cliente));

        var result = await handler.HandleAsync(new GetClientQuery(client.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
