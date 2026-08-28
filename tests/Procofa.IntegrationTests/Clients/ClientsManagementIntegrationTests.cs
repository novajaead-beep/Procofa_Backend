using Microsoft.EntityFrameworkCore;
using Npgsql;
using Procofa.Application.UseCases.Clients.CreateClient;
using Procofa.Application.UseCases.Clients.GetClient;
using Procofa.Application.UseCases.Clients.ListClients;
using Procofa.Application.UseCases.Clients.UpdateClient;
using Procofa.Application.UseCases.Companies.CreateCompany;
using Procofa.Application.UseCases.Contacts.CreateContact;
using Procofa.Application.UseCases.Sites.CreateSite;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests.Clients;

/// <summary>
/// Tests de integración de clientes/empresas auditadas/sedes/contactos contra PostgreSQL 18 real
/// vía Testcontainers, corriendo el grafo REAL de Infrastructure (<see cref="ClientsHandlerFactory"/>)
/// como <c>procofa_app</c>. La verificación física usa <c>SuperuserConnectionString</c> únicamente
/// en la fase de assert.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class ClientsManagementIntegrationTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task CreateClient_ConProgramas_PersisteClientYClientPrograms()
    {
        var tenantId = await fixture.CreateTenantAsync("clients-create");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);

        var (handler, dbContext) = ClientsHandlerFactory.CreateCreateClientHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateClientCommand("Importadora Azteca", null, "AAA010101AAA", null, null, null, ["OEA", "CTPAT"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ClientId);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.Clients
            .Include(c => c.Programs)
            .SingleAsync(c => c.Id == result.ClientId);

        Assert.Equal("Importadora Azteca", persisted.LegalName);
        Assert.True(persisted.IsActive);
        Assert.Equal(2, persisted.Programs.Count);
    }

    [Fact]
    public async Task UpdateClient_ReemplazaProgramas_DejaSoloLosNuevos()
    {
        var tenantId = await fixture.CreateTenantAsync("clients-update-programs");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var clientId = await fixture.CreateClientAsync(tenantId, "Cliente Original");
        var oeaId = await fixture.GetCatalogIdByCodeAsync("programs", "OEA");
        await fixture.AssignClientProgramAsync(tenantId, clientId, oeaId);

        var (handler, dbContext) = ClientsHandlerFactory.CreateUpdateClientHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new UpdateClientCommand(clientId, "Cliente Actualizado", null, null, null, null, null, ["CTPAT"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.Clients.Include(c => c.Programs).SingleAsync(c => c.Id == clientId);

        Assert.Equal("Cliente Actualizado", persisted.LegalName);
        Assert.DoesNotContain(persisted.Programs, p => p.ProgramId == oeaId);
        var ctpatId = await fixture.GetCatalogIdByCodeAsync("programs", "CTPAT");
        Assert.Contains(persisted.Programs, p => p.ProgramId == ctpatId);
    }

    [Fact]
    public async Task CreateCompany_PersisteAuditedCompanyBajoElClient()
    {
        var tenantId = await fixture.CreateTenantAsync("companies-create");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var clientId = await fixture.CreateClientAsync(tenantId, "Cliente Con Empresa");

        var (handler, dbContext) = ClientsHandlerFactory.CreateCreateCompanyHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateCompanyCommand(clientId, null, "Planta Norte SA de CV", null, null, null, null, true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.AuditedCompanies.SingleAsync(c => c.Id == result.CompanyId);
        Assert.Equal(clientId, persisted.ClientId);
    }

    [Fact]
    public async Task CreateSite_PersisteCompanySiteBajoLaEmpresaCorrecta()
    {
        var tenantId = await fixture.CreateTenantAsync("sites-create");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var clientId = await fixture.CreateClientAsync(tenantId, "Cliente Con Sede");
        var companyId = await fixture.CreateAuditedCompanyAsync(tenantId, clientId, "Empresa Con Sede");

        var (handler, dbContext) = ClientsHandlerFactory.CreateCreateSiteHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateSiteCommand(
                clientId, companyId, "Almacén Central", "Blvd. Industrial 500", null, "Monterrey", "Nuevo León",
                "64000", "México", null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.CompanySites.SingleAsync(s => s.Id == result.SiteId);
        Assert.Equal(companyId, persisted.AuditedCompanyId);
    }

    [Fact]
    public async Task CreateContact_PersisteClientContactBajoElClientCorrecto()
    {
        var tenantId = await fixture.CreateTenantAsync("contacts-create");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var clientId = await fixture.CreateClientAsync(tenantId, "Cliente Con Contacto");

        var (handler, dbContext) = ClientsHandlerFactory.CreateCreateContactHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateContactCommand(clientId, null, "Laura", "Gómez", "Encargada de Comercio Exterior", "laura@procofa-test.invalid", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.ClientContacts.SingleAsync(c => c.Id == result.ContactId);
        Assert.Equal(clientId, persisted.ClientId);
    }

    [Fact]
    public async Task GetClient_DeOtroTenant_RlsLoHaceInvisible_DevuelveNotFound()
    {
        var tenantId = await fixture.CreateTenantAsync("clients-rls-a");
        var otherTenantId = await fixture.CreateTenantAsync("clients-rls-b");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-rls");
        var clientDeOtroTenant = await fixture.CreateClientAsync(otherTenantId, "Cliente de otro tenant");

        var (handler, dbContext) = ClientsHandlerFactory.CreateGetClientHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(new GetClientQuery(clientDeOtroTenant), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetClientError.NotFound, result.Error);
    }

    [Fact]
    public async Task ListClients_SoloDevuelveLosDelTenantActivo()
    {
        var tenantId = await fixture.CreateTenantAsync("clients-list-rls-a");
        var otherTenantId = await fixture.CreateTenantAsync("clients-list-rls-b");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-list");
        var clientDelTenant = await fixture.CreateClientAsync(tenantId, "Cliente Visible");
        var clientDeOtroTenant = await fixture.CreateClientAsync(otherTenantId, "Cliente De Otro Tenant");

        var (handler, dbContext) = ClientsHandlerFactory.CreateListClientsHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(new ListClientsQuery(null, null, null, 1, 25), CancellationToken.None);

        var visibleIds = result.Items.Select(i => i.Id).ToList();
        Assert.Contains(clientDelTenant, visibleIds);
        Assert.DoesNotContain(clientDeOtroTenant, visibleIds);
    }

    [Fact]
    public async Task CreateCompany_ConClientDeOtroTenant_EsRechazadoPorTrigger()
    {
        var tenantId = await fixture.CreateTenantAsync("companies-cross-tenant");
        var otherTenantId = await fixture.CreateTenantAsync("companies-cross-tenant-other");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var clientDeOtroTenant = await fixture.CreateClientAsync(otherTenantId, "Cliente de otro tenant");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var setTenant = connection.CreateCommand();
        setTenant.CommandText = "SELECT set_config('app.tenant_id', @tenantId, true);";
        setTenant.Parameters.AddWithValue("tenantId", tenantId.ToString());
        await setTenant.ExecuteNonQueryAsync();

        // El trigger enforce_same_tenant_references valida que audited_companies.client_id
        // pertenezca al mismo tenant que la sesión, aun cuando tenant_id de la fila coincida.
        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO public.audited_companies (id, tenant_id, client_id, legal_name, is_active)
            VALUES (@id, @tenantId, @clientId, @legalName, true);
            """;
        insert.Parameters.AddWithValue("id", Guid.NewGuid());
        insert.Parameters.AddWithValue("tenantId", tenantId);
        insert.Parameters.AddWithValue("clientId", clientDeOtroTenant);
        insert.Parameters.AddWithValue("legalName", "Empresa Cross-Tenant");

        await Assert.ThrowsAsync<PostgresException>(() => insert.ExecuteNonQueryAsync());
    }
}
