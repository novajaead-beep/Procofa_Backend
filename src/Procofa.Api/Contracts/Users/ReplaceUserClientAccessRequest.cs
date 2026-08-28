namespace Procofa.Api.Contracts.Users;

/// <summary>Body de <c>PUT /api/users/{userId}/client-access</c> — reemplaza el conjunto
/// completo.</summary>
public sealed record ReplaceUserClientAccessRequest(IReadOnlyCollection<Guid>? ClientIds);
