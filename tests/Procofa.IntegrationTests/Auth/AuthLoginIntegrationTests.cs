using Microsoft.EntityFrameworkCore;
using Procofa.Application.UseCases.Auth.Login;
using Procofa.Domain.Enums;
using Procofa.Infrastructure.Security;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests.Auth;

/// <summary>
/// Tests de integración de <c>POST /api/auth/login</c>, corriendo el grafo REAL de Infrastructure
/// (<see cref="AuthHandlerFactory"/>) contra PostgreSQL 18 vía Testcontainers, como
/// <c>procofa_app</c> — nunca como superusuario, para ejercer RLS/ACL de verdad. </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class AuthLoginIntegrationTests(PostgresBaselineFixture fixture)
{
    private const string Password = "una-contraseña-de-prueba-segura";

    [Fact]
    public async Task Login_ConCredencialesValidas_DevuelveTokenYPersisteRefreshTokenHasheado_YRegistraLoginSuccess()
    {
        var passwordHash = new PasswordHasherAdapter().HashPassword(Password);
        var email = $"login-ok.{Guid.NewGuid():N}@procofa-test.invalid";
        var userId = await fixture.CreateUserWithPasswordAsync(
            AuthHandlerFactory.ProcofaTenantId, email, passwordHash, "AUDITOR_LIDER");

        var (handler, dbContext) = AuthHandlerFactory.CreateLoginHandler(fixture);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new LoginCommand(email, Password, "203.0.113.10", "integration-tests"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.UserId);
        Assert.Contains("AUDITOR_LIDER", result.Roles);
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));

        // El refresh token NUNCA se persiste en texto plano — solo su hash.
       await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        var persisted = await verifyContext.RefreshTokens
            .Where(t => t.UserId == userId)
            .SingleAsync();

        Assert.NotEqual(result.RefreshToken, persisted.TokenHash);
        Assert.DoesNotContain(result.RefreshToken!, persisted.TokenHash, StringComparison.Ordinal);

        var loginSuccessLogs = await verifyContext.AccessLogs
            .Where(l => l.UserId == userId && l.EventType == AccessLogEventType.LoginSuccess)
            .CountAsync();
        Assert.Equal(1, loginSuccessLogs);
    }

    [Fact]
    public async Task Login_ConPasswordIncorrecta_RegistraLoginFailure_YNoPersisteRefreshToken()
    {
        var passwordHash = new PasswordHasherAdapter().HashPassword(Password);
        var email = $"login-fail.{Guid.NewGuid():N}@procofa-test.invalid";
        var userId = await fixture.CreateUserWithPasswordAsync(
            AuthHandlerFactory.ProcofaTenantId, email, passwordHash, "CONSULTOR");

        var (handler, dbContext) = AuthHandlerFactory.CreateLoginHandler(fixture);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(
            new LoginCommand(email, "contraseña-incorrecta", null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LoginError.InvalidCredentials, result.Error);

       await using var verifyContext = fixture.CreateDbContext(fixture.SuperuserConnectionString);

        var loginFailureLogs = await verifyContext.AccessLogs
            .Where(l => l.UserId == userId && l.EventType == AccessLogEventType.LoginFailure)
            .CountAsync();
        Assert.Equal(1, loginFailureLogs);

        var refreshTokenCount = await verifyContext.RefreshTokens.Where(t => t.UserId == userId).CountAsync();
        Assert.Equal(0, refreshTokenCount);
    }

    [Fact]
    public async Task Login_UsuarioDeOtroTenant_NoEsEncontrado_RlsSigueAislando()
    {
        var otherTenantId = await fixture.CreateTenantAsync("auth-otro-tenant");
        var passwordHash = new PasswordHasherAdapter().HashPassword(Password);
        var email = $"login-otro-tenant.{Guid.NewGuid():N}@procofa-test.invalid";

        // Usuario sembrado en un tenant DISTINTO al que resuelve ITenantContext
        // (fijo, Stage 1, siempre PROCOFA) — el handler nunca debe encontrarlo,
        // sea por el filtro explícito de tenant o por RLS si ese filtro fallara.
        await fixture.CreateUserWithPasswordAsync(otherTenantId, email, passwordHash, "AUDITOR_LIDER");

        var (handler, dbContext) = AuthHandlerFactory.CreateLoginHandler(fixture);
        await using var _ = dbContext;

        var result = await handler.HandleAsync(new LoginCommand(email, Password, null, null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LoginError.InvalidCredentials, result.Error);
    }
}
