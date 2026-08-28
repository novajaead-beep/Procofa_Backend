using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Sites.CreateSite;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Tests.Sites;

public sealed class CreateSiteCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static CreateSiteCommandHandler CreateHandler(
        FakeAuditedCompanyRepository companies, FakeCompanySiteRepository? sites = null) =>
        new(new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), companies, sites ?? new FakeCompanySiteRepository());

    private static AuditedCompany CreateCompany(Guid clientId) =>
        new(Guid.NewGuid(), TenantId, clientId, null, "Empresa", null, null, null, null, false);

    [Fact]
    public async Task CreateSite_ConCompanyValida_CreaElSitio()
    {
        var company = CreateCompany(Guid.NewGuid());
        var sites = new FakeCompanySiteRepository();
        var handler = CreateHandler(new FakeAuditedCompanyRepository(company), sites);

        var result = await handler.HandleAsync(
            new CreateSiteCommand(company.ClientId, company.Id, "Planta 1", "Av. Siempre Viva 123", null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(sites.Sites);
        Assert.Equal(company.Id, created.AuditedCompanyId);
    }

    [Fact]
    public async Task CreateSite_PerteneceALaCompanyCorrecta_NoALaDeOtroClient()
    {
        var companyA = CreateCompany(Guid.NewGuid());
        var companyB = CreateCompany(Guid.NewGuid());
        var sites = new FakeCompanySiteRepository();
        var handler = CreateHandler(new FakeAuditedCompanyRepository(companyA, companyB), sites);

        var result = await handler.HandleAsync(
            new CreateSiteCommand(companyB.ClientId, companyB.Id, "Planta B", "Dirección B", null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(sites.Sites);
        Assert.Equal(companyB.Id, created.AuditedCompanyId);
        Assert.NotEqual(companyA.Id, created.AuditedCompanyId);
    }

    [Fact]
    public async Task CreateSite_EvitaAccesoCruzado_CompanyNoPerteneceAlClientDeLaRuta()
    {
        var companyA = CreateCompany(Guid.NewGuid());
        var clientBId = Guid.NewGuid();
        var sites = new FakeCompanySiteRepository();
        var handler = CreateHandler(new FakeAuditedCompanyRepository(companyA), sites);

        // companyA pertenece a otro client — clientBId en la ruta no debe encontrarla.
        var result = await handler.HandleAsync(
            new CreateSiteCommand(clientBId, companyA.Id, "Planta", "Dirección", null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateSiteError.CompanyNotFound, result.Error);
        Assert.Empty(sites.Sites);
    }
}
