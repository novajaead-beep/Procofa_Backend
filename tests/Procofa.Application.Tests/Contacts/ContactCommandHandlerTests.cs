using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Contacts.CreateContact;
using Procofa.Application.UseCases.Contacts.UpdateContact;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Tests.Contacts;

public sealed class ContactCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static CreateContactCommandHandler CreateCreateHandler(
        FakeClientRepository clients, FakeAuditedCompanyRepository companies, FakeClientContactRepository contacts) =>
        new(new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), clients, companies, contacts);

    private static UpdateContactCommandHandler CreateUpdateHandler(
        FakeAuditedCompanyRepository companies, FakeClientContactRepository contacts) =>
        new(new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), companies, contacts);

    [Fact]
    public async Task CreateContact_ConDatosValidos_CreaElContacto()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var contacts = new FakeClientContactRepository();
        var handler = CreateCreateHandler(
            new FakeClientRepository(client), new FakeAuditedCompanyRepository(), contacts);

        var result = await handler.HandleAsync(
            new CreateContactCommand(client.Id, null, "Ana", "Pérez", "Gerente", "ana@procofa.com", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(contacts.Contacts);
        Assert.Equal(client.Id, created.ClientId);
    }

    [Fact]
    public async Task UpdateContact_ConDatosValidos_ActualizaLosCampos()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var contact = new ClientContact(Guid.NewGuid(), TenantId, client.Id, null, "Ana", "Pérez", null, null, null);
        var contacts = new FakeClientContactRepository(contact);
        var handler = CreateUpdateHandler(new FakeAuditedCompanyRepository(), contacts);

        var result = await handler.HandleAsync(
            new UpdateContactCommand(client.Id, contact.Id, null, "Ana María", "Pérez López", "Directora", null, "555-0000"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ana María", contacts.Contacts[0].FirstName);
        Assert.Equal("555-0000", contacts.Contacts[0].Phone);
    }

    [Fact]
    public async Task Contact_PerteneceAlClientCorrecto_NoResuelveBajoOtroClient()
    {
        var clientA = new Client(Guid.NewGuid(), TenantId, "Cliente A", null, null, null, null, null);
        var clientB = new Client(Guid.NewGuid(), TenantId, "Cliente B", null, null, null, null, null);
        var contact = new ClientContact(Guid.NewGuid(), TenantId, clientA.Id, null, "Ana", "Pérez", null, null, null);
        var contacts = new FakeClientContactRepository(contact);

        var lookupUnderWrongClient = await contacts.GetByIdAsync(TenantId, clientB.Id, contact.Id, CancellationToken.None);

        Assert.Null(lookupUnderWrongClient);
    }
}
