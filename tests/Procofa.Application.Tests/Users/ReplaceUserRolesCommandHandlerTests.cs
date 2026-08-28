using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Users.ReplaceUserRoles;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.Tests.Users;

/// <summary>Tests de <see cref="ReplaceUserRolesCommandHandler"/>.</summary>
public sealed class ReplaceUserRolesCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static User CreateUserWithRoles(params Role[] roles)
    {
        var user = new User(Guid.NewGuid(), TenantId, "usuario@procofa.com", "hash", "Ana", "Auditora", phone: null);
        foreach (var role in roles)
        {
            user.AddRole(new UserRole(TenantId, user.Id, role.Id, assignedByUserId: null));
        }

        return user;
    }

    private static ReplaceUserRolesCommandHandler CreateHandler(FakeUserRepository users, Guid currentUserId) =>
        new(
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            users,
            new FakeRoleRepository(),
            new FakeCurrentUser(currentUserId));

    [Fact]
    public async Task ReplaceRoles_ReemplazaElConjuntoCompleto()
    {
        var user = CreateUserWithRoles(InMemoryRoleCatalog.AuditorLider);
        var users = new FakeUserRepository(user);
        var handler = CreateHandler(users, currentUserId: Guid.NewGuid());

        var result = await handler.HandleAsync(
            new ReplaceUserRolesCommand(user.Id, ["AUDITOR_APOYO", "CONSULTOR"]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var roleIds = users.Users[0].Roles.Select(r => r.RoleId).ToArray();
        Assert.Equal(2, roleIds.Length);
        Assert.Contains(InMemoryRoleCatalog.AuditorApoyo.Id, roleIds);
        Assert.Contains(InMemoryRoleCatalog.Consultor.Id, roleIds);
        Assert.DoesNotContain(InMemoryRoleCatalog.AuditorLider.Id, roleIds); // el rol anterior quedó fuera.
    }

    [Fact]
    public async Task ReplaceRoles_ConRolFueraDelCatalogoPermitido_Falla()
    {
        var user = CreateUserWithRoles(InMemoryRoleCatalog.Consultor);
        var users = new FakeUserRepository(user);
        var handler = CreateHandler(users, currentUserId: Guid.NewGuid());

        var result = await handler.HandleAsync(
            new ReplaceUserRolesCommand(user.Id, ["ROL_INVENTADO"]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceUserRolesError.ValidationFailed, result.Error);
        Assert.Contains(InMemoryRoleCatalog.Consultor.Id, users.Users[0].Roles.Select(r => r.RoleId)); // no se tocó.
    }

    [Fact]
    public async Task ReplaceRoles_UnAdminNoPuedeQuitarseSuPropioRolAdmin()
    {
        var admin = CreateUserWithRoles(InMemoryRoleCatalog.Admin);
        var users = new FakeUserRepository(admin);
        var handler = CreateHandler(users, currentUserId: admin.Id);

        var result = await handler.HandleAsync(
            new ReplaceUserRolesCommand(admin.Id, ["AUDITOR_LIDER"]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceUserRolesError.CannotRemoveOwnAdminRole, result.Error);
        Assert.Contains(InMemoryRoleCatalog.Admin.Id, users.Users[0].Roles.Select(r => r.RoleId)); // no se tocó.
    }

    [Fact]
    public async Task ReplaceRoles_SiElNuevoConjuntoYaNoIncluyeCliente_LimpiaClientAccess()
    {
        var user = CreateUserWithRoles(InMemoryRoleCatalog.Cliente);
        user.GrantClientAccess(new UserClientAccess(TenantId, user.Id, Guid.NewGuid(), grantedByUserId: null));
        var users = new FakeUserRepository(user);
        var handler = CreateHandler(users, currentUserId: Guid.NewGuid());

        var result = await handler.HandleAsync(
            new ReplaceUserRolesCommand(user.Id, ["AUDITOR_APOYO"]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(users.Users[0].ClientAccess);
    }
}
