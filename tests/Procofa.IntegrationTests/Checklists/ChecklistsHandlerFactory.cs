using Procofa.Application.UseCases.ChecklistSections.CreateChecklistSection;
using Procofa.Application.UseCases.ChecklistSections.DeleteChecklistSection;
using Procofa.Application.UseCases.ChecklistSections.UpdateChecklistSection;
using Procofa.Application.UseCases.Checklists.CreateChecklist;
using Procofa.Application.UseCases.Checklists.ResolveChecklist;
using Procofa.Application.UseCases.Checklists.UpdateChecklist;
using Procofa.Application.UseCases.ChecklistVersions.CreateChecklistVersion;
using Procofa.Application.UseCases.ChecklistVersions.PublishChecklistVersion;
using Procofa.Application.UseCases.ChecklistVersions.UpdateChecklistVersion;
using Procofa.Application.UseCases.Criteria.CreateCriterion;
using Procofa.Application.UseCases.Criteria.DeleteCriterion;
using Procofa.Application.UseCases.Criteria.UpdateCriterion;
using Procofa.Infrastructure;
using Procofa.Infrastructure.Persistence;
using Procofa.Infrastructure.Persistence.Repositories;
using Procofa.Infrastructure.Persistence.Tenancy;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;
using Procofa.IntegrationTests.Users;

namespace Procofa.IntegrationTests.Checklists;

/// <summary>Ensambla los casos de uso de Checklists/Versions/Sections/Criteria con las
/// implementaciones REALES de Infrastructure contra el contenedor Postgres desechable — mismo
/// patrón que <see cref="Clients.ClientsHandlerFactory"/>.</summary>
public static class ChecklistsHandlerFactory
{
    public static (CreateChecklistCommandHandler Handler, ProcofaDbContext DbContext) CreateCreateChecklistHandler(
        PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new CreateChecklistCommandHandler(
            tenantContext, unitOfWork, new ChecklistRepository(dbContext), new ProgramRepository(dbContext),
            new ProfileRepository(dbContext), new AuditTypeRepository(dbContext),
            new StaticCurrentUser(currentUserId, "ADMIN"));

        return (handler, dbContext);
    }

    public static (UpdateChecklistCommandHandler Handler, ProcofaDbContext DbContext) CreateUpdateChecklistHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new UpdateChecklistCommandHandler(
            tenantContext, unitOfWork, new ChecklistRepository(dbContext), new ProgramRepository(dbContext),
            new ProfileRepository(dbContext), new AuditTypeRepository(dbContext));

        return (handler, dbContext);
    }

    public static (ResolveChecklistQueryHandler Handler, ProcofaDbContext DbContext) CreateResolveChecklistHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new ResolveChecklistQueryHandler(
            tenantContext, unitOfWork, new ChecklistRepository(dbContext), new ChecklistVersionRepository(dbContext),
            new ProgramRepository(dbContext), new ProfileRepository(dbContext), new AuditTypeRepository(dbContext));

        return (handler, dbContext);
    }

    public static (CreateChecklistVersionCommandHandler Handler, ProcofaDbContext DbContext)
        CreateCreateChecklistVersionHandler(
            PostgresBaselineFixture fixture, Guid currentUserId, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new CreateChecklistVersionCommandHandler(
            tenantContext, unitOfWork, new ChecklistRepository(dbContext), new ChecklistVersionRepository(dbContext),
            new StaticCurrentUser(currentUserId, "ADMIN"));

        return (handler, dbContext);
    }

    public static (UpdateChecklistVersionCommandHandler Handler, ProcofaDbContext DbContext)
        CreateUpdateChecklistVersionHandler(PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new UpdateChecklistVersionCommandHandler(
            tenantContext, unitOfWork, new ChecklistVersionRepository(dbContext));

        return (handler, dbContext);
    }

    public static (PublishChecklistVersionCommandHandler Handler, ProcofaDbContext DbContext)
        CreatePublishChecklistVersionHandler(PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new PublishChecklistVersionCommandHandler(
            tenantContext, unitOfWork, new ChecklistVersionRepository(dbContext),
            new ChecklistSectionRepository(dbContext), new CriterionRepository(dbContext),
            new SystemDateTimeProvider());

        return (handler, dbContext);
    }

    public static (CreateChecklistSectionCommandHandler Handler, ProcofaDbContext DbContext)
        CreateCreateChecklistSectionHandler(PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new CreateChecklistSectionCommandHandler(
            tenantContext, unitOfWork, new ChecklistVersionRepository(dbContext),
            new ChecklistSectionRepository(dbContext));

        return (handler, dbContext);
    }

    public static (UpdateChecklistSectionCommandHandler Handler, ProcofaDbContext DbContext)
        CreateUpdateChecklistSectionHandler(PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new UpdateChecklistSectionCommandHandler(
            tenantContext, unitOfWork, new ChecklistVersionRepository(dbContext),
            new ChecklistSectionRepository(dbContext));

        return (handler, dbContext);
    }

    public static (DeleteChecklistSectionCommandHandler Handler, ProcofaDbContext DbContext)
        CreateDeleteChecklistSectionHandler(PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new DeleteChecklistSectionCommandHandler(
            tenantContext, unitOfWork, new ChecklistVersionRepository(dbContext),
            new ChecklistSectionRepository(dbContext), new CriterionRepository(dbContext));

        return (handler, dbContext);
    }

    public static (CreateCriterionCommandHandler Handler, ProcofaDbContext DbContext) CreateCreateCriterionHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new CreateCriterionCommandHandler(
            tenantContext, unitOfWork, new ChecklistVersionRepository(dbContext),
            new ChecklistSectionRepository(dbContext), new CriterionRepository(dbContext));

        return (handler, dbContext);
    }

    public static (UpdateCriterionCommandHandler Handler, ProcofaDbContext DbContext) CreateUpdateCriterionHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new UpdateCriterionCommandHandler(
            tenantContext, unitOfWork, new ChecklistVersionRepository(dbContext),
            new ChecklistSectionRepository(dbContext), new CriterionRepository(dbContext));

        return (handler, dbContext);
    }

    public static (DeleteCriterionCommandHandler Handler, ProcofaDbContext DbContext) CreateDeleteCriterionHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var (tenantContext, unitOfWork, dbContext) = CreateTenantScope(fixture, settings);
        var handler = new DeleteCriterionCommandHandler(
            tenantContext, unitOfWork, new ChecklistVersionRepository(dbContext),
            new ChecklistSectionRepository(dbContext), new CriterionRepository(dbContext));

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
