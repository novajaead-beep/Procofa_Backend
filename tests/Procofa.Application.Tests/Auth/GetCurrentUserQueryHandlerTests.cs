using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Auth.GetCurrentUser;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.Tests.Auth;

/// <summary>Tests de <see cref="GetCurrentUserQueryHandler"/>. El usuario efectivo se resuelve
/// siempre desde <c>IUserRepository</c> (persistencia actual) — nunca desde claims cacheados del
/// JWT — así que un rol asignado después de emitido el access token debe reflejarse igual.</summary>
public sealed class GetCurrentUserQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static User CreateUser(params Role[] roles)
    {
        var user = new User(
            Guid.NewGuid(), TenantId, "actual@procofa.com", "hash", "Marta", "Actual", phone: "555-0100");
        foreach (var role in roles)
        {
            user.AddRole(new UserRole(TenantId, user.Id, role.Id, assignedByUserId: null));
        }

        return user;
    }

    private static GetCurrentUserQueryHandler CreateHandler(Guid currentUserId, params User[] seedUsers) =>
        new(
            new FakeCurrentUser(currentUserId),
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            new FakeUserRepository(seedUsers));

    [Fact]
    public async Task GetCurrentUser_ConUsuarioValido_DevuelveInformacionActual()
    {
        var user = CreateUser(InMemoryRoleCatalog.AuditorLider);
        var handler = CreateHandler(user.Id, user);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.FirstName, result.FirstName);
        Assert.Equal(user.LastName, result.LastName);
        Assert.Equal(user.Phone, result.Phone);
        Assert.Equal(user.MustChangePassword, result.MustChangePassword);
        Assert.Contains("AUDITOR_LIDER", result.Roles);
    }

    [Fact]
    public async Task GetCurrentUser_ConUsuarioInexistente_Falla()
    {
        var handler = CreateHandler(Guid.NewGuid());

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetCurrentUser_ConUsuarioInactivo_Falla()
    {
        var user = CreateUser(InMemoryRoleCatalog.Consultor);
        user.Deactivate();
        var handler = CreateHandler(user.Id, user);

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetCurrentUser_RolesSeResuelvenDesdePersistenciaActual()
    {
        var user = CreateUser(InMemoryRoleCatalog.Consultor);
        var handler = CreateHandler(user.Id, user);

        // Rol asignado DESPUÉS de construir el handler — simula un cambio de roles ocurrido tras
        // la emisión del access token vigente: la respuesta debe reflejar el estado actual, no un
        // snapshot congelado.
        user.AddRole(new UserRole(TenantId, user.Id, InMemoryRoleCatalog.AuditorLider.Id, assignedByUserId: null));

        var result = await handler.HandleAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Roles.Count);
        Assert.Contains("CONSULTOR", result.Roles);
        Assert.Contains("AUDITOR_LIDER", result.Roles);
    }
}
