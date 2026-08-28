namespace Procofa.Api.Contracts.Users;

/// <summary>Fila de <c>GET /api/users</c> — nunca incluye <c>password_hash</c>.</summary>
public sealed record UserListItemResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    bool IsActive,
    bool MustChangePassword,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAtUtc);

/// <summary>Respuesta de <c>GET /api/users</c>.</summary>
public sealed record UserListResponse(
    IReadOnlyCollection<UserListItemResponse> Items,
    int Page,
    int PageSize,
    int Total);
