namespace Procofa.Application.UseCases.Users.GetUser;

/// <summary><c>GET /api/users/{userId}</c>.</summary>
public sealed record GetUserQuery(Guid UserId);
