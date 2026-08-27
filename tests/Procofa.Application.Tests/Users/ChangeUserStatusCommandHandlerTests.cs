using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Users.ChangeUserStatus;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Tests.Users;

/// <summary>Tests de <see cref="ChangeUserStatusCommandHandler"/> (Instrucción 05, sección "ACTIVAR / DESACTIVAR").</summary>
public sealed class ChangeUserStatusCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static User CreateUser(bool isActive = true)
    {
        var user = new User(Guid.NewGuid(), TenantId, "auditor@procofa.com", "hash", "Ana", "Auditora", phone: null);
        if (!isActive)
        {
            user.Deactivate();
        }

        return user;
    }

    private static ChangeUserStatusCommandHandler CreateHandler(FakeUserRepository users, Guid currentUserId) =>
        new(new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), users, new FakeCurrentUser(currentUserId));

    [Fact]
    public async Task ChangeStatus_DesactivaUnUsuarioActivo()
    {
        var user = CreateUser(isActive: true);
        var users = new FakeUserRepository(user);
        var handler = CreateHandler(users, currentUserId: Guid.NewGuid());

        var result = await handler.HandleAsync(new ChangeUserStatusCommand(user.Id, IsActive: false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(users.Users[0].IsActive);
    }

    [Fact]
    public async Task ChangeStatus_ReactivaUnUsuarioInactivo()
    {
        var user = CreateUser(isActive: false);
        var users = new FakeUserRepository(user);
        var handler = CreateHandler(users, currentUserId: Guid.NewGuid());

        var result = await handler.HandleAsync(new ChangeUserStatusCommand(user.Id, IsActive: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(users.Users[0].IsActive);
    }

    [Fact]
    public async Task ChangeStatus_UnAdminNoPuedeDesactivarseASiMismo()
    {
        var admin = CreateUser(isActive: true);
        var users = new FakeUserRepository(admin);
        var handler = CreateHandler(users, currentUserId: admin.Id);

        var result = await handler.HandleAsync(new ChangeUserStatusCommand(admin.Id, IsActive: false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChangeUserStatusError.CannotDeactivateSelf, result.Error);
        Assert.True(users.Users[0].IsActive); // no se tocó.
    }

    [Fact]
    public async Task ChangeStatus_UsuarioInexistente_DevuelveNotFound()
    {
        var users = new FakeUserRepository();
        var handler = CreateHandler(users, currentUserId: Guid.NewGuid());

        var result = await handler.HandleAsync(new ChangeUserStatusCommand(Guid.NewGuid(), IsActive: false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChangeUserStatusError.NotFound, result.Error);
    }
}
