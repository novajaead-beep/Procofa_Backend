using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Users.CreateUser;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.Tests.Users;

/// <summary>Tests de <see cref="CreateUserCommandHandler"/>.</summary>
public sealed class CreateUserCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid AdminId = Guid.NewGuid();

    private static (CreateUserCommandHandler Handler, FakeUserRepository Users) CreateHandler(
        FakeUserRepository? users = null, FakeClientRepository? clients = null)
    {
        users ??= new FakeUserRepository();
        var handler = new CreateUserCommandHandler(
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            users,
            new FakeRoleRepository(),
            clients ?? new FakeClientRepository(),
            new FakePasswordHasher(PasswordVerificationResult.Success),
            new FakeCurrentUser(AdminId));

        return (handler, users);
    }

    private static CreateUserCommand ValidCommand(
        string email = "auditor@procofa.com",
        string[]? roles = null,
        Guid[]? clientIds = null) =>
        new(email, "Ana", "López", null, "PasswordTemporalSeguro123!", roles ?? ["AUDITOR_APOYO"], clientIds ?? []);

    [Fact]
    public async Task CreateUser_ConDatosValidos_CreaElUsuarioConRolAsignado()
    {
        var (handler, users) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.UserId);

        var created = Assert.Single(users.Users);
        Assert.Equal(result.UserId, created.Id);
        Assert.Equal(TenantId, created.TenantId);
        Assert.True(created.IsActive);
        Assert.True(created.MustChangePassword);
        Assert.Equal(0, created.FailedLoginAttempts);
        Assert.Contains(created.Roles, r => r.RoleId == InMemoryRoleCatalog.AuditorApoyo.Id);
    }

    [Fact]
    public async Task CreateUser_HasheaLaContraseñaTemporal_NuncaLaPersistaEnTextoPlano()
    {
        var (handler, users) = CreateHandler();
        const string temporaryPassword = "PasswordTemporalSeguro123!";

        await handler.HandleAsync(ValidCommand() with { TemporaryPassword = temporaryPassword }, CancellationToken.None);

        var created = Assert.Single(users.Users);
        Assert.NotEqual(temporaryPassword, created.PasswordHash);
        Assert.Equal($"hashed:{temporaryPassword}", created.PasswordHash);
    }

    [Fact]
    public async Task CreateUser_ConEmailYaExistenteEnElTenant_Falla()
    {
        var existing = new User(Guid.NewGuid(), TenantId, "duplicado@procofa.com", "hash", "X", "Y", phone: null);
        var (handler, users) = CreateHandler(new FakeUserRepository(existing));

        var result = await handler.HandleAsync(ValidCommand(email: "duplicado@procofa.com"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateUserError.EmailAlreadyExists, result.Error);
        Assert.Single(users.Users); // no se agregó un segundo usuario.
    }

    [Fact]
    public async Task CreateUser_ConRolInexistenteEnElCatalogo_Falla()
    {
        // Fuera del catálogo cerrado de UserRoleCodes.
        var (handler, users) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand(roles: ["ROL_INVENTADO"]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateUserError.ValidationFailed, result.Error);
        Assert.Empty(users.Users);
    }

    [Fact]
    public async Task CreateUser_ConRolCliente_AsignaLosClientIdsSolicitados()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente S.A.", null, null, null, null, null);
        var (handler, users) = CreateHandler(clients: new FakeClientRepository(client));

        var result = await handler.HandleAsync(
            ValidCommand(roles: ["CLIENTE"], clientIds: [client.Id]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var created = Assert.Single(users.Users);
        Assert.Contains(created.ClientAccess, a => a.ClientId == client.Id);
    }

    [Fact]
    public async Task CreateUser_SinRolCliente_ConClientIds_Falla()
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente S.A.", null, null, null, null, null);
        var (handler, users) = CreateHandler(clients: new FakeClientRepository(client));

        // Rol AUDITOR_APOYO (no CLIENTE) pero con clientIds -> viola "clientIds debe quedar vacío".
        var result = await handler.HandleAsync(
            ValidCommand(roles: ["AUDITOR_APOYO"], clientIds: [client.Id]), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CreateUserError.ValidationFailed, result.Error);
        Assert.Empty(users.Users);
    }
}
