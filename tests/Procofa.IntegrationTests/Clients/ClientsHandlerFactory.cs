using Procofa.Application.UseCases.Clients.CreateClient;
using Procofa.Application.UseCases.Clients.GetClient;
using Procofa.Application.UseCases.Clients.ListClients;
using Procofa.Application.UseCases.Clients.UpdateClient;
using Procofa.Application.UseCases.Companies.CreateCompany;
using Procofa.Application.UseCases.Contacts.CreateContact;
using Procofa.Application.UseCases.Sites.CreateSite;
using Procofa.Infrastructure;
using Procofa.Infrastructure.Persistence;
using Procofa.Infrastructure.Persistence.Repositories;
using Procofa.Infrastructure.Persistence.Tenancy;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;
using Procofa.IntegrationTests.Users;

namespace Procofa.IntegrationTests.Clients;

/// <summary>Ensambla los casos de uso de Clients/Companies/Sites/Contacts con las implementaciones
/// REALES de Infrastructure contra el contenedor Postgres desechable — mismo patrón que <see
/// cref="UsersHandlerFactory"/>.</summary>
public static class ClientsHandlerFactory
{
    public static (CreateClientCommandHandler Handler, ProcofaDbContext DbContext) CreateCreateClientHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new CreateClientCommandHandler(
            tenantContext, unitOfWork, new ClientRepository(dbContext), new ProgramRepository(dbContext));

        return (handler, dbContext);
    }

    public static (UpdateClientCommandHandler Handler, ProcofaDbContext DbContext) CreateUpdateClientHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new UpdateClientCommandHandler(
            tenantContext, unitOfWork, new ClientRepository(dbContext), new ProgramRepository(dbContext));

        return (handler, dbContext);
    }

    public static (GetClientQueryHandler Handler, ProcofaDbContext DbContext) CreateGetClientHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null,
        params string[] roles)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var effectiveRoles = roles.Length == 0 ? ["ADMIN"] : roles;
        var handler = new GetClientQueryHandler(
            tenantContext, unitOfWork, new ClientRepository(dbContext), new AuditedCompanyRepository(dbContext),
            new ProgramRepository(dbContext), new UserRepository(dbContext),
            new StaticCurrentUser(currentUserId, effectiveRoles));

        return (handler, dbContext);
    }

    public static (ListClientsQueryHandler Handler, ProcofaDbContext DbContext) CreateListClientsHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null,
        params string[] roles)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var effectiveRoles = roles.Length == 0 ? ["ADMIN"] : roles;
        var handler = new ListClientsQueryHandler(
            tenantContext, unitOfWork, new ClientRepository(dbContext), new AuditedCompanyRepository(dbContext),
            new UserRepository(dbContext), new StaticCurrentUser(currentUserId, effectiveRoles));

        return (handler, dbContext);
    }

    public static (CreateCompanyCommandHandler Handler, ProcofaDbContext DbContext) CreateCreateCompanyHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new CreateCompanyCommandHandler(
            tenantContext, unitOfWork, new ClientRepository(dbContext), new AuditedCompanyRepository(dbContext));

        return (handler, dbContext);
    }

    public static (CreateSiteCommandHandler Handler, ProcofaDbContext DbContext) CreateCreateSiteHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new CreateSiteCommandHandler(
            tenantContext, unitOfWork, new AuditedCompanyRepository(dbContext), new CompanySiteRepository(dbContext));

        return (handler, dbContext);
    }

    public static (CreateContactCommandHandler Handler, ProcofaDbContext DbContext) CreateCreateContactHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new CreateContactCommandHandler(
            tenantContext, unitOfWork, new ClientRepository(dbContext), new AuditedCompanyRepository(dbContext),
            new ClientContactRepository(dbContext));

        return (handler, dbContext);
    }

    private static (Stage1TenantContext TenantContext, TenantUnitOfWork UnitOfWork, ProcofaDbContext DbContext)
        CreateTenantScope(PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings)
    {
        var effectiveSettings = settings ?? AuthHandlerFactory.DefaultSettings();
        var dbContext = fixture.CreateDbContext(fixture.AppConnectionString);
        var tenantContext = new Stage1TenantContext(effectiveSettings.ProcofaTenantId);
        var unitOfWork = new TenantUnitOfWork(dbContext, tenantContext);

        return (tenantContext, unitOfWork, dbContext);
    }
}
