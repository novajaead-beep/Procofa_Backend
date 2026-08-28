using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Companies.CreateCompany;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Tests.Companies;

public sealed class CreateCompanyCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static CreateCompanyCommandHandler CreateHandler(
        FakeClientRepository clients, FakeAuditedCompanyRepository? companies = null) =>
        new(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), clients,
            companies ?? new FakeAuditedCompanyRepository());

    [Fact]
    public async Task CreateCompany_ConClientValido_CreaLaEmpresa()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var companies = new FakeAuditedCompanyRepository();
        var handler = CreateHandler(new FakeClientRepository(client), companies);

        var result = await handler.HandleAsync(
            new CreateCompanyCommand(client.Id, null, "Empresa Auditada", null, null, null, null, false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(companies.Companies);
        Assert.Equal(client.Id, created.ClientId);
    }

    [Fact]
    public async Task CreateCompany_ConClientInexistente_Falla()
    {
        var companies = new FakeAuditedCompanyRepository();
        var handler = CreateHandler(new FakeClientRepository(), companies);

        var result = await handler.HandleAsync(
            new CreateCompanyCommand(Guid.NewGuid(), null, "Empresa", null, null, null, null, false),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateCompanyError.ClientNotFound, result.Error);
        Assert.Empty(companies.Companies);
    }

    [Fact]
    public async Task CreateCompany_EvitaAccesoCruzadoEntreClients()
    {
        var clientA = new Client(Guid.NewGuid(), TenantId, "Cliente A", null, null, null, null, null);
        var clientB = new Client(Guid.NewGuid(), TenantId, "Cliente B", null, null, null, null, null);
        var companyOfA = new AuditedCompany(Guid.NewGuid(), TenantId, clientA.Id, null, "Empresa A", null, null, null, null, false);

        var companies = new FakeAuditedCompanyRepository(companyOfA);
        var handler = CreateHandler(new FakeClientRepository(clientA, clientB), companies);

        // La empresa creada bajo clientB nunca debe resolver como si perteneciera a clientA.
        var result = await handler.HandleAsync(
            new CreateCompanyCommand(clientB.Id, null, "Empresa B", null, null, null, null, false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var lookup = await companies.GetByIdAsync(TenantId, clientA.Id, result.CompanyId!.Value, CancellationToken.None);
        Assert.Null(lookup);
    }
}
