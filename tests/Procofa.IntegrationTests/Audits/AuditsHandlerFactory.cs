using Procofa.Application.UseCases.Audits.CreateAudit;
using Procofa.Application.UseCases.Audits.GetAudit;
using Procofa.Application.UseCases.Audits.ListAudits;
using Procofa.Application.UseCases.Audits.ReplaceAuditChecklists;
using Procofa.Application.UseCases.Audits.ReplaceAuditPrograms;
using Procofa.Application.UseCases.Audits.ReplaceAuditTeam;
using Procofa.Application.UseCases.Audits.UpdateAudit;
using Procofa.Infrastructure;
using Procofa.Infrastructure.Persistence;
using Procofa.Infrastructure.Persistence.Repositories;
using Procofa.Infrastructure.Persistence.Tenancy;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;
using Procofa.IntegrationTests.Users;

namespace Procofa.IntegrationTests.Audits;

/// <summary>Ensambla los 7 casos de uso de planificación de auditorías con las implementaciones
/// REALES de Infrastructure contra el contenedor Postgres desechable — mismo patrón que <see
/// cref="Checklists.ChecklistsHandlerFactory"/>.</summary>
public static class AuditsHandlerFactory
{
    public static (CreateAuditCommandHandler Handler, ProcofaDbContext DbContext) CreateCreateAuditHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new CreateAuditCommandHandler(
            tenantContext, unitOfWork, new AuditRepository(dbContext), new ClientRepository(dbContext),
            new AuditedCompanyRepository(dbContext), new CompanySiteRepository(dbContext),
            new AuditTypeRepository(dbContext), new ProfileRepository(dbContext), new ProgramRepository(dbContext),
            new AuditStatusRepository(dbContext), new StaticCurrentUser(currentUserId, "ADMIN"),
            new SystemDateTimeProvider());

        return (handler, dbContext);
    }

    public static (GetAuditQueryHandler Handler, ProcofaDbContext DbContext) CreateGetAuditHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null,
        params string[] roles)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var effectiveRoles = roles.Length == 0 ? ["ADMIN"] : roles;
        var handler = new GetAuditQueryHandler(
            tenantContext, unitOfWork, new AuditRepository(dbContext), new ProgramRepository(dbContext),
            new UserRepository(dbContext), new AuditChecklistRepository(dbContext),
            new StaticCurrentUser(currentUserId, effectiveRoles));

        return (handler, dbContext);
    }

    public static (ListAuditsQueryHandler Handler, ProcofaDbContext DbContext) CreateListAuditsHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null,
        params string[] roles)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var effectiveRoles = roles.Length == 0 ? ["ADMIN"] : roles;
        var handler = new ListAuditsQueryHandler(
            tenantContext, unitOfWork, new AuditRepository(dbContext), new UserRepository(dbContext),
            new StaticCurrentUser(currentUserId, effectiveRoles));

        return (handler, dbContext);
    }

    public static (UpdateAuditCommandHandler Handler, ProcofaDbContext DbContext) CreateUpdateAuditHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new UpdateAuditCommandHandler(
            tenantContext, unitOfWork, new AuditRepository(dbContext), new AuditedCompanyRepository(dbContext),
            new CompanySiteRepository(dbContext), new AuditTypeRepository(dbContext), new ProfileRepository(dbContext),
            new AuditChecklistRepository(dbContext));

        return (handler, dbContext);
    }

    public static (ReplaceAuditProgramsCommandHandler Handler, ProcofaDbContext DbContext)
        CreateReplaceAuditProgramsHandler(PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new ReplaceAuditProgramsCommandHandler(
            tenantContext, unitOfWork, new AuditRepository(dbContext), new ProgramRepository(dbContext),
            new AuditChecklistRepository(dbContext));

        return (handler, dbContext);
    }

    public static (ReplaceAuditTeamCommandHandler Handler, ProcofaDbContext DbContext) CreateReplaceAuditTeamHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new ReplaceAuditTeamCommandHandler(
            tenantContext, unitOfWork, new AuditRepository(dbContext), new UserRepository(dbContext),
            new StaticCurrentUser(currentUserId, "ADMIN"));

        return (handler, dbContext);
    }

    public static (ReplaceAuditChecklistsCommandHandler Handler, ProcofaDbContext DbContext)
        CreateReplaceAuditChecklistsHandler(PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new ReplaceAuditChecklistsCommandHandler(
            tenantContext, unitOfWork, new AuditRepository(dbContext), new ChecklistRepository(dbContext),
            new ChecklistVersionRepository(dbContext), new AuditChecklistRepository(dbContext));

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
