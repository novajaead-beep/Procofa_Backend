using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Clients.UpdateClient;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Clients.ValueObjects;

namespace Procofa.Application.Tests.Clients;

public sealed class UpdateClientCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static UpdateClientCommandHandler CreateHandler(FakeClientRepository clients) =>
        new(new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), clients, new FakeProgramRepository());

    [Fact]
    public async Task UpdateClient_ConDatosValidos_ActualizaLosCampos()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Nombre Viejo", null, null, null, null, null);
        var clients = new FakeClientRepository(client);
        var handler = CreateHandler(clients);

        var result = await handler.HandleAsync(
            new UpdateClientCommand(client.Id, "Nombre Nuevo", "Trade", "TAX1", "Industria", "Tipo", "Notas", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Nombre Nuevo", clients.Clients[0].LegalName);
        Assert.Equal("TAX1", clients.Clients[0].TaxId);
    }

    [Fact]
    public async Task UpdateClient_ConProgramCodesNoNulo_ReemplazaElConjuntoCompleto()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        client.ReplacePrograms([new ClientProgram(TenantId, client.Id, InMemoryProgramCatalog.Oea.Id)]);
        var clients = new FakeClientRepository(client);
        var handler = CreateHandler(clients);

        var result = await handler.HandleAsync(
            new UpdateClientCommand(client.Id, "Cliente", null, null, null, null, null, ["CTPAT"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var updated = clients.Clients[0];
        Assert.Single(updated.Programs);
        Assert.Contains(updated.Programs, p => p.ProgramId == InMemoryProgramCatalog.Ctpat.Id);
    }

    [Fact]
    public async Task UpdateClient_ConProgramCodesNulo_NoTocaLosProgramas()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        client.ReplacePrograms([new ClientProgram(TenantId, client.Id, InMemoryProgramCatalog.Oea.Id)]);
        var clients = new FakeClientRepository(client);
        var handler = CreateHandler(clients);

        var result = await handler.HandleAsync(
            new UpdateClientCommand(client.Id, "Cliente Renombrado", null, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(clients.Clients[0].Programs);
    }

    [Fact]
    public async Task UpdateClient_ClienteInexistente_DevuelveNotFound()
    {
        var clients = new FakeClientRepository();
        var handler = CreateHandler(clients);

        var result = await handler.HandleAsync(
            new UpdateClientCommand(Guid.NewGuid(), "X", null, null, null, null, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateClientError.NotFound, result.Error);
    }
}
