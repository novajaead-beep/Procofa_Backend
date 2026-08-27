namespace Procofa.Api.Contracts.Users;

/// <summary>Un acceso a cliente concedido — sección 3.2, ejemplo <c>clientAccess</c>.</summary>
public sealed record UserClientAccessResponse(Guid ClientId);

/// <summary>
/// Respuesta de <c>GET /api/users/{userId}</c> (Instrucción 05, sección 3.2).
/// Nunca incluye <c>password_hash</c>, refresh tokens ni password reset tokens.
/// </summary>
public sealed record UserDetailResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    bool IsActive,
    bool MustChangePassword,
    int FailedLoginAttempts,
    DateTime? LockedUntilUtc,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<UserClientAccessResponse> ClientAccess);
