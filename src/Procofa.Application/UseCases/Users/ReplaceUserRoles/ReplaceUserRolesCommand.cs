namespace Procofa.Application.UseCases.Users.ReplaceUserRoles;

/// <summary>Instrucción 05, <c>PUT /api/users/{userId}/roles</c> — reemplaza el conjunto completo de roles.</summary>
public sealed record ReplaceUserRolesCommand(Guid UserId, IReadOnlyCollection<string>? Roles);
