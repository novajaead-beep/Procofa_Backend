namespace Procofa.Application.UseCases.Users.ReplaceUserClientAccess;

/// <summary><c>PUT /api/users/{userId}/client-access</c> — reemplaza el conjunto completo de
/// accesos.</summary>
public sealed record ReplaceUserClientAccessCommand(Guid UserId, IReadOnlyCollection<Guid>? ClientIds);
