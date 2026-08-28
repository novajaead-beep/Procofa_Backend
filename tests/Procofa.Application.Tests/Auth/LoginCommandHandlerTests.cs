using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Auth.Login;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Identity.ValueObjects;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.Auth;

/// <summary>
/// Tests de <see cref="LoginCommandHandler"/>. Todos usan los fakes en memoria de
/// <c>TestDoubles</c> — sin BD real, sin transacción real (la <c>FakeTenantUnitOfWork</c> solo
/// ejecuta el delegado directamente). </summary>
public sealed class LoginCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly DateTime NowUtc = new(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
    private const string PlainPassword = "una-contraseña-cualquiera";
    private const string StoredHash = "hash-almacenado";

    private static User CreateActiveUser(params Role[] roles)
    {
        var user = new User(Guid.NewGuid(), TenantId, "auditor@procofa.com", StoredHash, "Ana", "Auditora", phone: null);
        foreach (var role in roles)
        {
            user.AddRole(new UserRole(TenantId, user.Id, role.Id, assignedByUserId: null));
        }

        return user;
    }

    private static (LoginCommandHandler Handler, FakeUserRepository Users, FakeAccessLogRepository AccessLogs, FakeRefreshTokenRepository RefreshTokens, FakeAccessTokenGenerator TokenGenerator)
        CreateHandler(PasswordVerificationResult verificationResult, params User[] seedUsers)
    {
        var users = new FakeUserRepository(seedUsers);
        var accessLogs = new FakeAccessLogRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var tokenGenerator = new FakeAccessTokenGenerator();

        var handler = new LoginCommandHandler(
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            users,
            accessLogs,
            refreshTokens,
            new FakePasswordHasher(verificationResult),
            tokenGenerator,
            new FakeRefreshTokenFactory(),
            new FakeAuthPolicyOptions(),
            new FakeDateTimeProvider(NowUtc));

        return (handler, users, accessLogs, refreshTokens, tokenGenerator);
    }

    [Fact]
    public async Task Login_ConCredencialesCorrectas_DevuelveTokenYRolesYRegistraLoginSuccess()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.AuditorLider);
        var (handler, _, accessLogs, refreshTokens, tokenGenerator) =
            CreateHandler(PasswordVerificationResult.Success, user);

        var result = await handler.HandleAsync(
            new LoginCommand(user.Email, PlainPassword, "127.0.0.1", "xunit-agent"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.UserId);
        Assert.Contains("AUDITOR_LIDER", result.Roles);
        Assert.NotNull(result.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        Assert.NotNull(tokenGenerator.LastRoleCodes);
        Assert.Single(tokenGenerator.LastRoleCodes!);
        Assert.Contains("AUDITOR_LIDER", tokenGenerator.LastRoleCodes!);

        Assert.Single(refreshTokens.Added);
        Assert.Single(accessLogs.Logged);
        Assert.Equal(AccessLogEventType.LoginSuccess, accessLogs.Logged[0].EventType);
    }

    [Fact]
    public async Task Login_ConPasswordIncorrecta_DevuelveInvalidCredentials_IncrementaIntentosYRegistraLoginFailure()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.Consultor);
        var (handler, users, accessLogs, refreshTokens, _) =
            CreateHandler(PasswordVerificationResult.Failed, user);

        var result = await handler.HandleAsync(
            new LoginCommand(user.Email, "password-incorrecta", null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LoginError.InvalidCredentials, result.Error);
        Assert.Equal(1, users.Users[0].FailedLoginAttempts);
        Assert.Empty(refreshTokens.Added);
        Assert.Single(accessLogs.Logged);
        Assert.Equal(AccessLogEventType.LoginFailure, accessLogs.Logged[0].EventType);
    }

    [Fact]
    public async Task Login_UsuarioInexistente_DevuelveElMismoErrorQuePasswordIncorrecta()
    {
        var (handler, _, accessLogs, _, _) = CreateHandler(PasswordVerificationResult.Failed);

        var result = await handler.HandleAsync(
            new LoginCommand("no-existe@procofa.com", PlainPassword, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LoginError.InvalidCredentials, result.Error);
        Assert.Single(accessLogs.Logged);
        Assert.Null(accessLogs.Logged[0].UserId);
    }

    [Fact]
    public async Task Login_UsuarioInactivo_DevuelveUserInactive()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.Consultor);
        // IsActive no tiene setter público — se fuerza vía reflexión para aislar este escenario
        // del resto del ciclo de vida del usuario.
        typeof(User).GetProperty(nameof(User.IsActive))!.SetValue(user, false);

        var (handler, _, accessLogs, _, _) = CreateHandler(PasswordVerificationResult.Success, user);

        var result = await handler.HandleAsync(
            new LoginCommand(user.Email, PlainPassword, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LoginError.UserInactive, result.Error);
        Assert.Single(accessLogs.Logged);
    }

    [Fact]
    public async Task Login_UsuarioBloqueado_DevuelveUserLocked_YNoIncrementaIntentos()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.Consultor);
        user.RegisterFailedLogin(maxFailedAttempts: 1, lockoutDuration: TimeSpan.FromMinutes(15), nowUtc: NowUtc.AddMinutes(-1));
        Assert.True(user.IsLockedOut(NowUtc));

        var (handler, users, _, _, _) = CreateHandler(PasswordVerificationResult.Success, user);

        var result = await handler.HandleAsync(
            new LoginCommand(user.Email, PlainPassword, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LoginError.UserLocked, result.Error);
        // El intento no se cuenta mientras ya está bloqueado (evita extender el lockout indefinidamente).
        Assert.Equal(1, users.Users[0].FailedLoginAttempts);
    }

    [Fact]
    public async Task Login_AlAlcanzarElMaximoDeIntentos_AplicaLockout()
    {
        var user = CreateActiveUser(InMemoryRoleCatalog.Consultor);
        var users = new FakeUserRepository(user);
        var handler = new LoginCommandHandler(
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            users,
            new FakeAccessLogRepository(),
            new FakeRefreshTokenRepository(),
            new FakePasswordHasher(PasswordVerificationResult.Failed),
            new FakeAccessTokenGenerator(),
            new FakeRefreshTokenFactory(),
            new FakeAuthPolicyOptions { MaxFailedLoginAttempts = 3 },
            new FakeDateTimeProvider(NowUtc));

        for (var i = 0; i < 3; i++)
        {
            await handler.HandleAsync(new LoginCommand(user.Email, "mal", null, null), CancellationToken.None);
        }

        Assert.Equal(3, users.Users[0].FailedLoginAttempts);
        Assert.True(users.Users[0].IsLockedOut(NowUtc));
    }

    [Fact]
    public void LoginCommand_NuncaExponeUnCampoDeTenant()
    {
        var propiedadesSospechosas = typeof(LoginCommand)
            .GetProperties()
            .Where(p => p.Name.Contains("tenant", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            propiedadesSospechosas.Length == 0,
            "LoginCommand no debe exponer ningún campo relacionado con tenant — el tenant se " +
                "resuelve exclusivamente desde ITenantContext.");
    }
}
