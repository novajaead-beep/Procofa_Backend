using Microsoft.EntityFrameworkCore;
using Procofa.Application.UseCases.Auth.BootstrapAdmin;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests.Auth;

/// <summary>
/// Tests de integración del bootstrap one-shot del primer ADMIN, contra PostgreSQL 18 real vía
/// Testcontainers. Cada test usa su propio tenant (vía <see
/// cref="PostgresBaselineFixture.CreateTenantAsync"/>) para no interferir entre sí ni con el tenant
/// fijo PROCOFA que otros tests puedan usar. </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class BootstrapAdminIntegrationTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task Bootstrap_PrimeraEjecucion_CreaElAdminConRolAsignado()
    {
        var tenantId = await fixture.CreateTenantAsync("bootstrap-primera");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var (handler, dbContext) = AuthHandlerFactory.CreateBootstrapAdminHandler(fixture, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new BootstrapAdminCommand("admin@procofa.com", "una-contraseña-segura-de-verdad", "Admin", "PROCOFA"),
            CancellationToken.None);

        Assert.Equal(BootstrapAdminOutcome.Created, result.Outcome);
        Assert.NotNull(result.UserId);

        // Verificación física: SuperuserConnectionString a propósito (no
        // AppConnectionString). Esta conexión NUNCA pasa por
        // ITenantUnitOfWork.ExecuteWriteAsync/ExecuteReadAsync, así que jamás
        // ejecuta `SET LOCAL app.tenant_id` — con procofa_app y FORCE ROW
        // LEVEL SECURITY, cualquier SELECT en esa conexión vería 0 filas
        // siempre (current_setting('app.tenant_id', true) nulo), sin importar
        // si el handler persistió correctamente. Usar el superusuario aquí
        // bypassa RLS y confirma el estado físico real, no un falso negativo.
        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var adminRoleId = await fixture.GetCatalogIdByCodeAsync("roles", "ADMIN");

        var hasAdminRole = await verifyContext.Users
            .Where(u => u.Id == result.UserId)
            .SelectMany(u => u.Roles)
            .AnyAsync(r => r.RoleId == adminRoleId);

        Assert.True(hasAdminRole);
    }

    [Fact]
    public async Task Bootstrap_SegundaEjecucion_NoDuplicaElAdmin_EsIdempotente()
    {
        var tenantId = await fixture.CreateTenantAsync("bootstrap-segunda");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);

        var (firstHandler, firstDbContext) = AuthHandlerFactory.CreateBootstrapAdminHandler(fixture, settings);
        await using (firstDbContext)
        {
            var firstResult = await firstHandler.HandleAsync(
                new BootstrapAdminCommand("admin@procofa.com", "una-contraseña-segura-de-verdad", "Admin", "PROCOFA"),
                CancellationToken.None);
            Assert.Equal(BootstrapAdminOutcome.Created, firstResult.Outcome);
        }

        var (secondHandler, secondDbContext) = AuthHandlerFactory.CreateBootstrapAdminHandler(fixture, settings);
        await using (secondDbContext)
        {
            var secondResult = await secondHandler.HandleAsync(
                new BootstrapAdminCommand("otro-admin@procofa.com", "otra-contraseña-segura-de-verdad", "Otro", "Admin"),
                CancellationToken.None);
            Assert.Equal(BootstrapAdminOutcome.AlreadyExists, secondResult.Outcome);
        }

        // Mismo motivo que en el test anterior: SuperuserConnectionString
        // para verificación física, bypass de RLS a propósito.
        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var adminRoleId = await fixture.GetCatalogIdByCodeAsync("roles", "ADMIN");

        var adminCount = await verifyContext.Users
            .Where(u => u.TenantId == tenantId)
            .SelectMany(u => u.Roles)
            .Where(r => r.RoleId == adminRoleId)
            .CountAsync();

        Assert.Equal(1, adminCount); // sigue habiendo un solo ADMIN — la segunda ejecución no duplicó.
    }
}
