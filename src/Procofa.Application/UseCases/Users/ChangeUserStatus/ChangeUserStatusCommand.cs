namespace Procofa.Application.UseCases.Users.ChangeUserStatus;

/// <summary>Instrucción 05, <c>PATCH /api/users/{userId}/status</c>.</summary>
public sealed record ChangeUserStatusCommand(Guid UserId, bool IsActive);
