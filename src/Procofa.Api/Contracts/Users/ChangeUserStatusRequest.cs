namespace Procofa.Api.Contracts.Users;

/// <summary>Body de <c>PATCH /api/users/{userId}/status</c> (Instrucción 05, sección 6).</summary>
public sealed record ChangeUserStatusRequest(bool IsActive);
