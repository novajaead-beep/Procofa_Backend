using Procofa.Application.UseCases.Auth.BootstrapAdmin;
using Procofa.Application.UseCases.Auth.Login;
using Procofa.Infrastructure;
using Procofa.Infrastructure.Persistence;
using Procofa.Infrastructure.Persistence.Repositories;
using Procofa.Infrastructure.Persistence.Tenancy;
using Procofa.Infrastructure.Security;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests.Auth;

/// <summary>
/// Ensambla <see cref="LoginCommandHandler"/>/<see cref="BootstrapAdminCommandHandler"/>
/// con las implementaciones REALES de Infrastructure (nunca fakes) contra el
/// contenedor Postgres desechable — el mismo grafo que arma
/// <c>Procofa.Infrastructure.DependencyInjection.AddInfrastructure</c>, pero
/// construido a mano aquí para no depender de un <c>IServiceProvider</c>
/// completo en los tests. Todas las queries corren como <c>procofa_app</c>
/// (<see cref="PostgresBaselineFixture.AppConnectionString"/>) para ejercer
/// RLS/ACL de verdad (Instrucción 03, sección 27).
/// </summary>
public static class AuthHandlerFactory
{
    /// <summary>Tenant PROCOFA Stage 1 — mismo GUID fijo sembrado por 003_seed_catalogs.sql.</summary>
    public static readonly Guid ProcofaTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static InfrastructureAuthSettings DefaultSettings(Guid? tenantId = null) => new(
        ProcofaTenantId: tenantId ?? ProcofaTenantId,
        JwtIssuer: "procofa-integration-tests",
        JwtAudience: "procofa-integration-tests",
        JwtSigningKey: "clave-de-firma-de-pruebas-de-integracion-32b+",
        JwtAccessTokenMinutes: 15,
        AuthMaxFailedLoginAttempts: 5,
        AuthLockoutMinutes: 15,
        AuthRefreshTokenDays: 30);

    public static (LoginCommandHandler Handler, ProcofaDbContext DbContext) CreateLoginHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var effectiveSettings = settings ?? DefaultSettings();
        var dbContext = fixture.CreateDbContext(fixture.AppConnectionString);
        var tenantContext = new Stage1TenantContext(effectiveSettings.ProcofaTenantId);
        var unitOfWork = new TenantUnitOfWork(dbContext, tenantContext);

        var handler = new LoginCommandHandler(
            tenantContext,
            unitOfWork,
            new UserRepository(dbContext),
            new AccessLogRepository(dbContext),
            new RefreshTokenRepository(dbContext),
            new PasswordHasherAdapter(),
            new JwtAccessTokenGenerator(
                effectiveSettings.JwtIssuer, effectiveSettings.JwtAudience,
                effectiveSettings.JwtSigningKey, effectiveSettings.JwtAccessTokenMinutes),
            new RefreshTokenFactory(),
            new AuthPolicyOptionsAdapter(effectiveSettings),
            new SystemDateTimeProvider());

        return (handler, dbContext);
    }

    public static (BootstrapAdminCommandHandler Handler, ProcofaDbContext DbContext) CreateBootstrapAdminHandler(
        PostgresBaselineFixture fixture, InfrastructureAuthSettings? settings = null)
    {
        var effectiveSettings = settings ?? DefaultSettings();
        var dbContext = fixture.CreateDbContext(fixture.AppConnectionString);
        var tenantContext = new Stage1TenantContext(effectiveSettings.ProcofaTenantId);
        var unitOfWork = new TenantUnitOfWork(dbContext, tenantContext);

        var handler = new BootstrapAdminCommandHandler(
            tenantContext,
            unitOfWork,
            new UserRepository(dbContext),
            new RoleRepository(dbContext),
            new PasswordHasherAdapter());

        return (handler, dbContext);
    }
}
