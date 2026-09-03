using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Auth.RefreshSession;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Application.Tests.Auth;

/// <summary>
/// Tests de <see cref="RefreshSessionCommandHandler"/>. Todos usan los fakes en memoria de
/// <c>TestDoubles</c> — sin BD real. Ningún assert imprime ni compara el valor crudo del refresh
/// token contra un literal reconocible como secreto: solo se verifica igualdad/desigualdad entre
/// valores devueltos por el propio handler. </summary>
public sealed class RefreshSessionCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly DateTime NowUtc = new(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
    private const string ValidRawToken = "raw-token-valido";

    private static User CreateActiveUser(params Role[] roles)
    {
        var user = new User(Guid.NewGuid(), TenantId, "auditor@procofa.com", "hash", "Ana", "Auditora", phone: null);
        foreach (var role in roles)
        {
            user.AddRole(new UserRole(TenantId, user.Id, role.Id, assignedByUserId: null));
        }

        return user;
    }

    private static RefreshToken SeedActiveToken(Guid userId, string rawToken, DateTime expiresAtUtc) =>
        new(Guid.NewGuid(), TenantId, userId, new FakeRefreshTokenFactory().Hash(rawToken), expiresAtUtc);

    private static (RefreshSessionCommandHandler Handler, FakeUserRepository Users, FakeRefreshTokenRepository RefreshTokens, FakeAccessTokenGenerator TokenGenerator)
        CreateHandler(FakeAuthPolicyOptions? authPolicyOptions = null, params User[] seedUsers)
    {
        var users = new FakeUserRepository(seedUsers);
        var refreshTokens = new FakeRefreshTokenRepository();
        var tokenGenerator = new FakeAccessTokenGenerator();

        var handler = new RefreshSessionCommandHandler(
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            users,
            refreshTokens,
            tokenGenerator,
            new FakeRefreshTokenFactory(),
            authPolicyOptions ?? new FakeAuthPolicyOptions(),
            new FakeDateTimeProvider(NowUtc));

        return (handler, users, refreshTokens, tokenGenerator);
    }

    [Fact]
    public async Task Refresh_ConTokenValido_RotaCorrectamente()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.AuditorLider);
        var currentToken = SeedActiveToken(user.Id, ValidRawToken, NowUtc.AddDays(10));
        var (handler, _, refreshTokens, _) = CreateHandler(seedUsers: user);
        refreshTokens.Added.Add(currentToken);

        var result = await handler.HandleAsync(new RefreshSessionCommand(ValidRawToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(currentToken.IsRevoked);
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.NotEqual(ValidRawToken, result.RefreshToken);
        Assert.Equal(2, refreshTokens.Added.Count);

        var newToken = Assert.Single(refreshTokens.Added, t => t != currentToken);
        Assert.False(newToken.IsRevoked);
        Assert.Equal(user.Id, newToken.UserId);
    }

    [Fact]
    public async Task Refresh_ConTokenInexistente_Falla()
    {
        var (handler, _, _, _) = CreateHandler();

        var result = await handler.HandleAsync(new RefreshSessionCommand("token-nunca-emitido"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshSessionError.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task Refresh_ConTokenExpirado_FallaYLoRevoca()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.Consultor);
        var expiredToken = SeedActiveToken(user.Id, ValidRawToken, NowUtc.AddMinutes(-1));
        var (handler, _, refreshTokens, _) = CreateHandler(seedUsers: user);
        refreshTokens.Added.Add(expiredToken);

        var result = await handler.HandleAsync(new RefreshSessionCommand(ValidRawToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshSessionError.InvalidRefreshToken, result.Error);
        Assert.True(expiredToken.IsRevoked);
        Assert.Single(refreshTokens.Added);
    }

    [Fact]
    public async Task Refresh_ConTokenRevocado_Falla()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.Consultor);
        var revokedToken = SeedActiveToken(user.Id, ValidRawToken, NowUtc.AddDays(10));
        revokedToken.Revoke(NowUtc.AddMinutes(-5));
        var (handler, _, refreshTokens, _) = CreateHandler(seedUsers: user);
        refreshTokens.Added.Add(revokedToken);

        var result = await handler.HandleAsync(new RefreshSessionCommand(ValidRawToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshSessionError.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task Refresh_ReuseDeTokenRevocado_RevocaTokensActivosDelUsuario()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.Consultor);
        var revokedToken = SeedActiveToken(user.Id, ValidRawToken, NowUtc.AddDays(10));
        revokedToken.Revoke(NowUtc.AddMinutes(-5));
        var otherActiveToken = SeedActiveToken(user.Id, "otro-raw-token-activo", NowUtc.AddDays(10));

        var (handler, _, refreshTokens, _) = CreateHandler(seedUsers: user);
        refreshTokens.Added.Add(revokedToken);
        refreshTokens.Added.Add(otherActiveToken);

        var result = await handler.HandleAsync(new RefreshSessionCommand(ValidRawToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(otherActiveToken.IsRevoked);
    }

    [Fact]
    public async Task Refresh_ConUsuarioInexistente_Falla()
    {
        var orphanToken = SeedActiveToken(Guid.NewGuid(), ValidRawToken, NowUtc.AddDays(10));
        var (handler, _, refreshTokens, _) = CreateHandler();
        refreshTokens.Added.Add(orphanToken);

        var result = await handler.HandleAsync(new RefreshSessionCommand(ValidRawToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshSessionError.InvalidRefreshToken, result.Error);
    }

    [Fact]
    public async Task Refresh_ConUsuarioInactivo_FallaYRevocaTokensActivosDelUsuario()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.Consultor);
        user.Deactivate();
        var currentToken = SeedActiveToken(user.Id, ValidRawToken, NowUtc.AddDays(10));
        var otherActiveToken = SeedActiveToken(user.Id, "otro-raw-token-activo", NowUtc.AddDays(10));

        var (handler, _, refreshTokens, _) = CreateHandler(seedUsers: user);
        refreshTokens.Added.Add(currentToken);
        refreshTokens.Added.Add(otherActiveToken);

        var result = await handler.HandleAsync(new RefreshSessionCommand(ValidRawToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RefreshSessionError.InvalidRefreshToken, result.Error);
        Assert.True(currentToken.IsRevoked);
        Assert.True(otherActiveToken.IsRevoked);
    }

    [Fact]
    public async Task Refresh_NuevoAccessToken_UsaRolesActualesDelUsuario()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.AuditorLider, InMemoryRoleCatalog.Consultor);
        var currentToken = SeedActiveToken(user.Id, ValidRawToken, NowUtc.AddDays(10));
        var (handler, _, refreshTokens, tokenGenerator) = CreateHandler(seedUsers: user);
        refreshTokens.Added.Add(currentToken);

        var result = await handler.HandleAsync(new RefreshSessionCommand(ValidRawToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(tokenGenerator.LastRoleCodes);
        Assert.Equal(2, tokenGenerator.LastRoleCodes!.Count);
        Assert.Contains("AUDITOR_LIDER", tokenGenerator.LastRoleCodes!);
        Assert.Contains("CONSULTOR", tokenGenerator.LastRoleCodes!);
        Assert.Contains("AUDITOR_LIDER", result.Roles);
        Assert.Contains("CONSULTOR", result.Roles);
    }

    [Fact]
    public async Task Refresh_NuevaExpiracion_UsaRefreshTokenLifetimeConfigurado()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.Consultor);
        var currentToken = SeedActiveToken(user.Id, ValidRawToken, NowUtc.AddDays(10));
        var authPolicyOptions = new FakeAuthPolicyOptions { RefreshTokenLifetime = TimeSpan.FromDays(7) };
        var (handler, _, refreshTokens, _) = CreateHandler(authPolicyOptions, user);
        refreshTokens.Added.Add(currentToken);

        var result = await handler.HandleAsync(new RefreshSessionCommand(ValidRawToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NowUtc.AddDays(7), result.RefreshTokenExpiresAtUtc);
    }
}
