using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Audits.CreateAudit;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Tests.UseCases.Audits;

public sealed class CreateAuditCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly DateOnly ScheduledDate = new(2026, 3, 1);

    private sealed record Fixture(
        CreateAuditCommandHandler Handler,
        FakeAuditRepository Audits,
        Guid ClientId,
        Guid CompanyId,
        Guid SiteId,
        Guid OtherClientCompanyId,
        Guid OtherCompanySiteId);

    private static Fixture CreateHandler()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente OEA", null, null, null, null, null);
        var otherClient = new Client(Guid.NewGuid(), TenantId, "Otro Cliente", null, null, null, null, null);

        var company = new AuditedCompany(
            Guid.NewGuid(), TenantId, client.Id, null, "Planta Norte", null, null, null, null, false);
        var companyOfOtherClient = new AuditedCompany(
            Guid.NewGuid(), TenantId, otherClient.Id, null, "Planta de otro cliente", null, null, null, null, false);

        var site = new CompanySite(
            Guid.NewGuid(), TenantId, company.Id, "Almacén Central", "Calle 1", null, null, null, null, "México",
            null, null);
        var siteOfOtherCompany = new CompanySite(
            Guid.NewGuid(), TenantId, companyOfOtherClient.Id, "Otra sede", "Calle 2", null, null, null, null,
            "México", null, null);

        var audits = new FakeAuditRepository();
        var handler = new CreateAuditCommandHandler(
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            audits,
            new FakeClientRepository(client, otherClient),
            new FakeAuditedCompanyRepository(company, companyOfOtherClient),
            new FakeCompanySiteRepository(site, siteOfOtherCompany),
            new FakeAuditTypeRepository(),
            new FakeProfileRepository(),
            new FakeProgramRepository(),
            new FakeAuditStatusRepository(),
            new FakeCurrentUser(UserId, "ADMIN"),
            new FakeDateTimeProvider(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        return new Fixture(
            handler, audits, client.Id, company.Id, site.Id, companyOfOtherClient.Id, siteOfOtherCompany.Id);
    }

    private static CreateAuditCommand ValidCommand(
        Fixture fixture, Guid? companySiteId, string executionMode, string[]? programCodes = null) => new(
        fixture.ClientId, fixture.CompanyId, companySiteId, InMemoryAuditTypeCatalog.InternaOea.Id,
        InMemoryProfileCatalog.Maquila.Id, programCodes ?? [], "Objetivo de prueba", "Alcance de prueba", null,
        ScheduledDate, executionMode);

    [Fact]
    public async Task CreateAudit_Onsite_ConSitio_CreaLaAuditoria()
    {
        var fixture = CreateHandler();

        var result = await fixture.Handler.HandleAsync(
            ValidCommand(fixture, fixture.SiteId, "ONSITE"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(fixture.Audits.Audits);
        Assert.Equal(fixture.SiteId, created.CompanySiteId);
        Assert.StartsWith("AUD-", created.Folio);
    }

    [Fact]
    public async Task CreateAudit_Onsite_SinSitio_Falla()
    {
        var fixture = CreateHandler();

        var result = await fixture.Handler.HandleAsync(
            ValidCommand(fixture, null, "ONSITE"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateAuditError.ValidationFailed, result.Error);
        Assert.Empty(fixture.Audits.Audits);
    }

    [Fact]
    public async Task CreateAudit_Hybrid_SinSitio_Falla()
    {
        var fixture = CreateHandler();

        var result = await fixture.Handler.HandleAsync(
            ValidCommand(fixture, null, "HYBRID"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateAuditError.ValidationFailed, result.Error);
        Assert.Empty(fixture.Audits.Audits);
    }

    [Fact]
    public async Task CreateAudit_Remote_SinSitio_CreaLaAuditoria()
    {
        var fixture = CreateHandler();

        var result = await fixture.Handler.HandleAsync(
            ValidCommand(fixture, null, "REMOTE"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(fixture.Audits.Audits);
        Assert.Null(created.CompanySiteId);
    }

    [Fact]
    public async Task CreateAudit_EmpresaDeOtroCliente_Falla()
    {
        var fixture = CreateHandler();
        var command = ValidCommand(fixture, fixture.SiteId, "ONSITE") with
        {
            AuditedCompanyId = fixture.OtherClientCompanyId,
        };

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateAuditError.AuditedCompanyNotFound, result.Error);
        Assert.Empty(fixture.Audits.Audits);
    }

    [Fact]
    public async Task CreateAudit_SitioDeOtraEmpresa_Falla()
    {
        var fixture = CreateHandler();
        var command = ValidCommand(fixture, fixture.OtherCompanySiteId, "ONSITE");

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateAuditError.CompanySiteNotFound, result.Error);
        Assert.Empty(fixture.Audits.Audits);
    }

    [Fact]
    public async Task CreateAudit_ProgramaInexistente_Falla()
    {
        var fixture = CreateHandler();
        var command = ValidCommand(fixture, fixture.SiteId, "ONSITE", ["NO_EXISTE"]);

        var result = await fixture.Handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateAuditError.ProgramNotFound, result.Error);
        Assert.Empty(fixture.Audits.Audits);
    }
}
