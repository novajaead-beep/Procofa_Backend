namespace Procofa.Api.Contracts.Users;

/// <summary>Body de <c>PUT /api/users/{userId}/roles</c> (Instrucción 05, sección 7) — reemplaza el conjunto completo.</summary>
public sealed record ReplaceUserRolesRequest(IReadOnlyCollection<string>? Roles);
