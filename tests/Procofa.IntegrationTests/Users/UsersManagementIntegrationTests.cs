using Microsoft.EntityFrameworkCore;
using Procofa.Application.UseCases.Users.ChangeUserStatus;
using Procofa.Application.UseCases.Users.CreateUser;
using Procofa.Application.UseCases.Users.ReplaceUserRoles;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests.Users;

/// <summary>
/// Tests de integración de gestión de usuarios (Instrucción 05, sección 15),
/// contra PostgreSQL 18 real vía Testcontainers, corriendo el grafo REAL de
/// Infrastructure (<see cref="UsersHandlerFactory"/>) como <c>procofa_app</c>.
/// La verificación física usa <c>SuperuserConnectionString</c> ÚNICAMENTE en
/// la fase de assert (mismo motivo que <c>BootstrapAdminIntegrationTests</c>:
/// una conexión <c>procofa_app</c> nueva, sin <c>SET LOCAL app.tenant_id</c>,
/// vería 0 filas por RLS incluso si la escritura fue correcta).
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class UsersManagementIntegrationTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task CreateUser_PersisteUsersYUserRoles()
    {
        var tenantId = await fixture.CreateTenantAsync("users-create");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-creador");

        var (handler, dbContext) = UsersHandlerFactory.CreateCreateUserHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateUserCommand(
                "nuevo.auditor@procofa.com", "Nuevo", "Auditor", null,
                "PasswordTemporalSeguro123!", ["AUDITOR_APOYO"], []),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UserId);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var auditorApoyoRoleId = await fixture.GetCatalogIdByCodeAsync("roles", "AUDITOR_APOYO");

        var persistedUser = await verifyContext.Users.SingleAsync(u => u.Id == result.UserId);
        Assert.True(persistedUser.MustChangePassword);
        Assert.True(persistedUser.IsActive);

        var hasRole = await verifyContext.Users
            .Where(u => u.Id == result.UserId)
            .SelectMany(u => u.Roles)
            .AnyAsync(r => r.RoleId == auditorApoyoRoleId);
        Assert.True(hasRole);
    }

    [Fact]
    public async Task CreateUser_ConRolCliente_PersisteUsersUserRolesYUserClientAccess()
    {
        var tenantId = await fixture.CreateTenantAsync("users-create-cliente");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-creador");
        var clientId = await fixture.CreateClientAsync(tenantId, "Cliente de prueba");

        var (handler, dbContext) = UsersHandlerFactory.CreateCreateUserHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateUserCommand(
                "nuevo.cliente@procofa.com", "Nuevo", "Cliente", null,
                "PasswordTemporalSeguro123!", ["CLIENTE"], [clientId]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var clienteRoleId = await fixture.GetCatalogIdByCodeAsync("roles", "CLIENTE");

        var user = await verifyContext.Users
            .Include(u => u.Roles)
            .Include(u => u.ClientAccess)
            .AsSplitQuery()
            .SingleAsync(u => u.Id == result.UserId);

        Assert.Contains(user.Roles, r => r.RoleId == clienteRoleId);
        Assert.Contains(user.ClientAccess, a => a.ClientId == clientId);
    }

    [Fact]
    public async Task CreateUser_ConClientDeOtroTenant_RlsImpideVincularlo()
    {
        var tenantId = await fixture.CreateTenantAsync("users-create-rls");
        var otherTenantId = await fixture.CreateTenantAsync("users-create-rls-otro");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin-creador");
        var clientDeOtroTenant = await fixture.CreateClientAsync(otherTenantId, "Cliente de otro tenant");

        var (handler, dbContext) = UsersHandlerFactory.CreateCreateUserHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new CreateUserCommand(
                "no-deberia-crearse@procofa.com", "No", "Debería", null,
                "PasswordTemporalSeguro123!", ["CLIENTE"], [clientDeOtroTenant]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateUserError.ClientNotFound, result.Error);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var existsAnyway = await verifyContext.Users.AnyAsync(u => u.Email == "no-deberia-crearse@procofa.com");
        Assert.False(existsAnyway); // nada se persistió — ni el usuario ni el vínculo.
    }

    [Fact]
    public async Task ChangeUserStatus_Desactivar_PersisteIsActiveFalse()
    {
        var tenantId = await fixture.CreateTenantAsync("users-status");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin");
        var target = await fixture.CreateUserAsync(tenantId, "objetivo");

        var (handler, dbContext) = UsersHandlerFactory.CreateChangeUserStatusHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(new ChangeUserStatusCommand(target, IsActive: false), CancellationToken.None);
        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.Users.SingleAsync(u => u.Id == target);
        Assert.False(persisted.IsActive);
    }

    [Fact]
    public async Task ReplaceUserRoles_EliminaLosRolesAnterioresYDejaSoloLosNuevos()
    {
        var tenantId = await fixture.CreateTenantAsync("users-roles");
        var settings = AuthHandlerFactory.DefaultSettings(tenantId);
        var admin = await fixture.CreateUserAsync(tenantId, "admin");
        var target = await fixture.CreateUserWithPasswordAsync(
            tenantId, $"objetivo.{Guid.NewGuid():N}@procofa-test.invalid", "hash-de-prueba", "AUDITOR_LIDER");

        var (handler, dbContext) = UsersHandlerFactory.CreateReplaceUserRolesHandler(fixture, admin, settings);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new ReplaceUserRolesCommand(target, ["CONSULTOR", "AUDITOR_APOYO"]), CancellationToken.None);
        Assert.True(result.IsSuccess);

        await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var auditorLiderRoleId = await fixture.GetCatalogIdByCodeAsync("roles", "AUDITOR_LIDER");
        var consultorRoleId = await fixture.GetCatalogIdByCodeAsync("roles", "CONSULTOR");
        var auditorApoyoRoleId = await fixture.GetCatalogIdByCodeAsync("roles", "AUDITOR_APOYO");

        var roleIds = await verifyContext.Users
            .Where(u => u.Id == target)
            .SelectMany(u => u.Roles)
            .Select(r => r.RoleId)
            .ToListAsync();

        Assert.DoesNotContain(auditorLiderRoleId, roleIds); // el rol original quedó eliminado.
        Assert.Contains(consultorRoleId, roleIds);
        Assert.Contains(auditorApoyoRoleId, roleIds);
        Assert.Equal(2, roleIds.Count);
    }
}
