using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Auth.Logout;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.Auth;

/// <summary>
/// Tests de <see cref="LogoutCommandHandler"/>. Logout es deliberadamente idempotente: ausencia de
/// token, token inexistente y token ya revocado son todos no-ops exitosos — nunca un error. </summary>
public sealed class LogoutCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly DateTime NowUtc = new(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc);
    private const string ValidRawToken = "raw-token-de-logout";

    private static RefreshToken SeedActiveToken(Guid userId, string rawToken, DateTime expiresAtUtc) =>
        new(Guid.NewGuid(), TenantId, userId, new FakeRefreshTokenFactory().Hash(rawToken), expiresAtUtc);

    private static (LogoutCommandHandler Handler, FakeRefreshTokenRepository RefreshTokens, FakeAccessLogRepository AccessLogs)
        CreateHandler()
    {
        var refreshTokens = new FakeRefreshTokenRepository();
        var accessLogs = new FakeAccessLogRepository();

        var handler = new LogoutCommandHandler(
            new FakeTenantContext(TenantId),
            new FakeTenantUnitOfWork(),
            refreshTokens,
            new FakeRefreshTokenFactory(),
            accessLogs,
            new FakeDateTimeProvider(NowUtc));

        return (handler, refreshTokens, accessLogs);
    }

    [Fact]
    public async Task Logout_ConTokenValido_LoRevoca()
    {
        var userId = Guid.NewGuid();
        var token = SeedActiveToken(userId, ValidRawToken, NowUtc.AddDays(10));
        var (handler, refreshTokens, _) = CreateHandler();
        refreshTokens.Added.Add(token);

        await handler.HandleAsync(new LogoutCommand(ValidRawToken, "127.0.0.1", "xunit-agent"), CancellationToken.None);

        Assert.True(token.IsRevoked);
    }

    [Fact]
    public async Task Logout_ConTokenValido_GeneraAccessLogLogout()
    {
        var userId = Guid.NewGuid();
        var token = SeedActiveToken(userId, ValidRawToken, NowUtc.AddDays(10));
        var (handler, refreshTokens, accessLogs) = CreateHandler();
        refreshTokens.Added.Add(token);

        await handler.HandleAsync(new LogoutCommand(ValidRawToken, "127.0.0.1", "xunit-agent"), CancellationToken.None);

        var log = Assert.Single(accessLogs.Logged);
        Assert.Equal(AccessLogEventType.Logout, log.EventType);
        Assert.Equal(userId, log.UserId);
    }

    [Fact]
    public async Task Logout_ConTokenInexistente_EsIdempotente()
    {
        var (handler, refreshTokens, accessLogs) = CreateHandler();

        var exception = await Record.ExceptionAsync(() =>
            handler.HandleAsync(new LogoutCommand("token-nunca-emitido", null, null), CancellationToken.None));

        Assert.Null(exception);
        Assert.Empty(refreshTokens.Added);
        Assert.Empty(accessLogs.Logged);
    }

    [Fact]
    public async Task Logout_ConTokenYaRevocado_EsIdempotente()
    {
        var userId = Guid.NewGuid();
        var token = SeedActiveToken(userId, ValidRawToken, NowUtc.AddDays(10));
        token.Revoke(NowUtc.AddMinutes(-5));
        var revokedAt = token.RevokedAtUtc;
        var (handler, refreshTokens, accessLogs) = CreateHandler();
        refreshTokens.Added.Add(token);

        var exception = await Record.ExceptionAsync(() =>
            handler.HandleAsync(new LogoutCommand(ValidRawToken, null, null), CancellationToken.None));

        Assert.Null(exception);
        Assert.Equal(revokedAt, token.RevokedAtUtc);
        Assert.Empty(accessLogs.Logged);
    }

    [Fact]
    public async Task Logout_SinRefreshToken_EsIdempotente()
    {
        var (handler, refreshTokens, accessLogs) = CreateHandler();

        var exception = await Record.ExceptionAsync(() =>
            handler.HandleAsync(new LogoutCommand(null, null, null), CancellationToken.None));

        Assert.Null(exception);
        Assert.Empty(refreshTokens.Added);
        Assert.Empty(accessLogs.Logged);
    }
}
