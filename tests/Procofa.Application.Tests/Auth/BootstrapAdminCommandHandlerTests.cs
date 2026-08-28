using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Auth.BootstrapAdmin;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.Tests.Auth;

/// <summary>Tests de <see cref="BootstrapAdminCommandHandler"/>.</summary>
public sealed class BootstrapAdminCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static BootstrapAdminCommandHandler CreateHandler(FakeUserRepository users) =>
        new(
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            users,
            new FakeRoleRepository(),
            new FakePasswordHasher(PasswordVerificationResult.Success));

    [Fact]
    public async Task Bootstrap_ConDatosValidos_CreaElPrimerAdminConRolAsignado()
    {
        var users = new FakeUserRepository();
        var handler = CreateHandler(users);

        var result = await handler.HandleAsync(
            new BootstrapAdminCommand("admin@procofa.com", "una-contraseña-segura", "Admin", "PROCOFA"),
            CancellationToken.None);

        Assert.Equal(BootstrapAdminOutcome.Created, result.Outcome);
        Assert.NotNull(result.UserId);

        var created = Assert.Single(users.Users);
        Assert.Equal(result.UserId, created.Id);
        Assert.Equal(TenantId, created.TenantId);
        Assert.Contains(created.Roles, r => r.RoleId == InMemoryRoleCatalog.Admin.Id);
    }

    [Fact]
    public async Task Bootstrap_CuandoYaExisteUnAdmin_EsIdempotente_NoCreaOtro()
    {
        var existingAdmin = new User(
            Guid.NewGuid(), TenantId, "primer-admin@procofa.com", "hash", "Primer", "Admin", phone: null);
        existingAdmin.AddRole(new UserRole(TenantId, existingAdmin.Id, InMemoryRoleCatalog.Admin.Id, assignedByUserId: null));

        var users = new FakeUserRepository(existingAdmin);
        var handler = CreateHandler(users);

        var result = await handler.HandleAsync(
            new BootstrapAdminCommand("otro-admin@procofa.com", "otra-contraseña-segura", "Otro", "Admin"),
            CancellationToken.None);

        Assert.Equal(BootstrapAdminOutcome.AlreadyExists, result.Outcome);
        Assert.Null(result.UserId);
        Assert.Single(users.Users); // sigue habiendo solo el admin original — no se duplicó.
    }

    [Theory]
    [InlineData("", "una-contraseña-segura", "Admin", "PROCOFA")]
    [InlineData("admin@procofa.com", "corta", "Admin", "PROCOFA")]
    [InlineData("admin@procofa.com", "una-contraseña-segura", "", "PROCOFA")]
    public async Task Bootstrap_ConDatosInvalidos_FallaDeFormaSegura_SinCrearNada(
        string email, string password, string firstName, string lastName)
    {
        var users = new FakeUserRepository();
        var handler = CreateHandler(users);

        var result = await handler.HandleAsync(
            new BootstrapAdminCommand(email, password, firstName, lastName), CancellationToken.None);

        Assert.Equal(BootstrapAdminOutcome.ValidationFailed, result.Outcome);
        Assert.False(string.IsNullOrWhiteSpace(result.ValidationError));
        Assert.Empty(users.Users);
    }
}
