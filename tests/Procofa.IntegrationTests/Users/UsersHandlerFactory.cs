using Procofa.Application.UseCases.Users.ChangeUserStatus;
using Procofa.Application.UseCases.Users.CreateUser;
using Procofa.Application.UseCases.Users.GetUser;
using Procofa.Application.UseCases.Users.ListUsers;
using Procofa.Application.UseCases.Users.ReplaceUserClientAccess;
using Procofa.Application.UseCases.Users.ReplaceUserRoles;
using Procofa.Infrastructure;
using Procofa.Infrastructure.Persistence;
using Procofa.Infrastructure.Persistence.Repositories;
using Procofa.Infrastructure.Persistence.Tenancy;
using Procofa.Infrastructure.Security;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests.Users;

/// <summary>
/// Ensambla los casos de uso de gestión de usuarios (Instrucción 05) con las
/// implementaciones REALES de Infrastructure contra el contenedor Postgres
/// desechable — mismo patrón que <see cref="AuthHandlerFactory"/>: siempre
/// como <c>procofa_app</c> (nunca superusuario) para ejercer RLS/ACL de
/// verdad.
/// </summary>
public static class UsersHandlerFactory
{
    public static (CreateUserCommandHandler Handler, ProcofaDbContext DbContext) CreateCreateUserHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);

        var handler = new CreateUserCommandHandler(
            tenantContext,
            unitOfWork,
            new UserRepository(dbContext),
            new RoleRepository(dbContext),
            new ClientRepository(dbContext),
            new PasswordHasherAdapter(),
            new StaticCurrentUser(currentUserId));

        return (handler, dbContext);
    }

    public static (ChangeUserStatusCommandHandler Handler, ProcofaDbContext DbContext) CreateChangeUserStatusHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);

        var handler = new ChangeUserStatusCommandHandler(
            tenantContext, unitOfWork, new UserRepository(dbContext), new StaticCurrentUser(currentUserId));

        return (handler, dbContext);
    }

    public static (ReplaceUserRolesCommandHandler Handler, ProcofaDbContext DbContext) CreateReplaceUserRolesHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);

        var handler = new ReplaceUserRolesCommandHandler(
            tenantContext,
            unitOfWork,
            new UserRepository(dbContext),
            new RoleRepository(dbContext),
            new StaticCurrentUser(currentUserId));

        return (handler, dbContext);
    }

    public static (ReplaceUserClientAccessCommandHandler Handler, ProcofaDbContext DbContext) CreateReplaceUserClientAccessHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);

        var handler = new ReplaceUserClientAccessCommandHandler(
            tenantContext,
            unitOfWork,
            new UserRepository(dbContext),
            new RoleRepository(dbContext),
            new ClientRepository(dbContext),
            new StaticCurrentUser(currentUserId));

        return (handler, dbContext);
    }

    public static (GetUserQueryHandler Handler, ProcofaDbContext DbContext) CreateGetUserHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        return (new GetUserQueryHandler(tenantContext, unitOfWork, new UserRepository(dbContext)), dbContext);
    }

    public static (ListUsersQueryHandler Handler, ProcofaDbContext DbContext) CreateListUsersHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        return (new ListUsersQueryHandler(tenantContext, unitOfWork, new UserRepository(dbContext)), dbContext);
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
