namespace Procofa.Application.UseCases.Users.GetUser;

/// <summary>Instrucción 05, <c>GET /api/users/{userId}</c>.</summary>
public sealed record GetUserQuery(Guid UserId);
