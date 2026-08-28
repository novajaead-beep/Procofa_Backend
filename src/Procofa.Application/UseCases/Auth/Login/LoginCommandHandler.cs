using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Enums;

namespace Procofa.Application.UseCases.Auth.Login;

/// <summary>
/// Caso de uso <c>POST /api/auth/login</c>. Orquesta el flujo completo dentro de UNA transacción
/// tenant-scoped (<see cref="ITenantUnitOfWork.ExecuteWriteAsync{T}"/>): resolver tenant → buscar
/// usuario → validar estado/lockout → verificar contraseña → actualizar intentos/lockout → emitir
/// tokens → registrar access_log → commit. Ningún paso individual hace su propio commit — o todo el
/// login se confirma junto, o nada. </summary>
public sealed class LoginCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository,
    IAccessLogRepository accessLogRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IAccessTokenGenerator accessTokenGenerator,
    IRefreshTokenFactory refreshTokenFactory,
    IAuthPolicyOptions authPolicyOptions,
    IDateTimeProvider dateTimeProvider)
{
    public Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        // resuelto SIEMPRE desde configuración (ITenantContext), nunca desde el LoginCommand.
        var tenantId = tenantContext.TenantId;
        var normalizedEmail = User.Normalize(command.Email);
        var nowUtc = dateTimeProvider.UtcNow;

        return unitOfWork.ExecuteWriteAsync(
            ct => ExecuteAsync(tenantId, normalizedEmail, nowUtc, command, ct),
            cancellationToken);
    }

    private async Task<LoginResult> ExecuteAsync(
        Guid tenantId, string normalizedEmail, DateTime nowUtc, LoginCommand command, CancellationToken ct)
    {
        var user = await userRepository.FindByNormalizedEmailAsync(tenantId, normalizedEmail, ct);

        if (user is null)
        {
            // Paso 4: usuario inexistente -> LOGIN_FAILURE, sin revelar la
            // no-existencia (mismo LoginError que password incorrecto).
            await LogAsync(tenantId, null, command, AccessLogEventType.LoginFailure, ct);
            return LoginResult.Failure(LoginError.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            // Paso 5.
            await LogAsync(tenantId, user.Id, command, AccessLogEventType.LoginFailure, ct);
            return LoginResult.Failure(LoginError.UserInactive);
        }

        if (user.IsLockedOut(nowUtc))
        {
            // Paso 6: lockout vigente -> rechazo sin tocar el contador (evita
            // extender el lockout indefinidamente ante reintentos mientras está bloqueado).
            await LogAsync(tenantId, user.Id, command, AccessLogEventType.LoginFailure, ct);
            return LoginResult.Failure(LoginError.UserLocked);
        }

        // Paso 7: PasswordHasher<TUser> (o equivalente) vía el puerto IPasswordHasher.
        var verification = passwordHasher.VerifyPassword(user.PasswordHash, command.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            // Paso 8: incrementar intentos, aplicar lockout si corresponde,
            // LOGIN_FAILURE — misma transacción (SaveChanges ocurre al salir
            // de ExecuteWriteAsync, junto con el AccessLog).
            user.RegisterFailedLogin(
                authPolicyOptions.MaxFailedLoginAttempts, authPolicyOptions.LockoutDuration, nowUtc);
            await LogAsync(tenantId, user.Id, command, AccessLogEventType.LoginFailure, ct);
            return LoginResult.Failure(LoginError.InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.HashPassword(command.Password));
        }

        // Paso 9: resetear intentos/lockout, emitir tokens, LOGIN_SUCCESS, commit.
        user.RegisterSuccessfulLogin(nowUtc);

        var roleIds = user.Roles.Select(r => r.RoleId).ToArray();
        var roleCodes = await userRepository.GetRoleCodesAsync(roleIds, ct);

        var accessToken = accessTokenGenerator.GenerateAccessToken(user.Id, tenantId, user.Email, roleCodes);

        var generatedRefreshToken = refreshTokenFactory.Create();
        var refreshTokenExpiresAtUtc = nowUtc.Add(authPolicyOptions.RefreshTokenLifetime);
        var refreshToken = new RefreshToken(
            Guid.NewGuid(), tenantId, user.Id, generatedRefreshToken.TokenHash, refreshTokenExpiresAtUtc);
        await refreshTokenRepository.AddAsync(refreshToken, ct);

        await LogAsync(tenantId, user.Id, command, AccessLogEventType.LoginSuccess, ct);

        return LoginResult.Success(
            user.Id, roleCodes, accessToken, generatedRefreshToken.RawToken, refreshTokenExpiresAtUtc);
    }

    private Task LogAsync(
        Guid tenantId, Guid? userId, LoginCommand command, AccessLogEventType eventType, CancellationToken ct)
    {
        var accessLog = new AccessLog(
            Guid.NewGuid(), tenantId, userId, command.Email, eventType, command.IpAddress, command.UserAgent);
        return accessLogRepository.AddAsync(accessLog, ct);
    }
}
