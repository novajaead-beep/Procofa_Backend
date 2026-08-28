namespace Procofa.Api.Contracts.Users;

/// <summary>Body de <c>PATCH /api/users/{userId}/status</c>.</summary>
public sealed record ChangeUserStatusRequest(bool IsActive);
