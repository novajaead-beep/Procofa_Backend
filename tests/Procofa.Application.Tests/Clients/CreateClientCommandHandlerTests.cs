using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Clients.CreateClient;

namespace Procofa.Application.Tests.Clients;

public sealed class CreateClientCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static (CreateClientCommandHandler Handler, FakeClientRepository Clients) CreateHandler(
        FakeClientRepository? clients = null)
    {
        clients ??= new FakeClientRepository();
        var handler = new CreateClientCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), clients, new FakeProgramRepository());

        return (handler, clients);
    }

    private static CreateClientCommand ValidCommand(string[]? programs = null) =>
        new("Cliente S.A. de C.V.", "Cliente", "TAX123", "Manufactura", "Maquiladora", "Notas", programs);

    [Fact]
    public async Task CreateClient_ConDatosValidos_CreaElCliente()
    {
        var (handler, clients) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(clients.Clients);
        Assert.Equal(result.ClientId, created.Id);
        Assert.Equal(TenantId, created.TenantId);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task CreateClient_AsignaOea_QuedaEnProgramas()
    {
        var (handler, clients) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand(["OEA"]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(clients.Clients);
        Assert.Contains(created.Programs, p => p.ProgramId == InMemoryProgramCatalog.Oea.Id);
    }

    [Fact]
    public async Task CreateClient_AsignaOeaYCtpat_QuedanAmbosEnProgramas()
    {
        var (handler, clients) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand(["OEA", "CTPAT"]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(clients.Clients);
        Assert.Equal(2, created.Programs.Count);
        Assert.Contains(created.Programs, p => p.ProgramId == InMemoryProgramCatalog.Oea.Id);
        Assert.Contains(created.Programs, p => p.ProgramId == InMemoryProgramCatalog.Ctpat.Id);
    }

    [Fact]
    public async Task CreateClient_ConProgramaInvalido_Falla()
    {
        var (handler, clients) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand(["ISO9001"]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateClientError.ProgramNotFound, result.Error);
        Assert.Empty(clients.Clients);
    }

    [Fact]
    public async Task CreateClient_SinLegalName_Falla()
    {
        var (handler, clients) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand() with { LegalName = null }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateClientError.ValidationFailed, result.Error);
        Assert.Empty(clients.Clients);
    }

    [Fact]
    public async Task CreateClient_ConTaxIdYaExistenteEnElTenant_Falla()
    {
        var existing = new Domain.Entities.Clients.Client(
            Guid.NewGuid(), TenantId, "Otro Cliente", null, "TAX123", null, null, null);
        var (handler, clients) = CreateHandler(new FakeClientRepository(existing));

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateClientError.TaxIdAlreadyExists, result.Error);
        Assert.Single(clients.Clients);
    }
}
