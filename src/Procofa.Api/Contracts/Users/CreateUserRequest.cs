namespace Procofa.Api.Contracts.Users;

/// <summary>Body de <c>POST /api/users</c> (Instrucción 05, sección 4). Todo nullable a propósito — la validación de "obligatorio" ocurre explícitamente en Application, nunca vía excepción de binding.</summary>
public sealed record CreateUserRequest(
    string? Email,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? TemporaryPassword,
    IReadOnlyCollection<string>? Roles,
    IReadOnlyCollection<Guid>? ClientIds);

/// <summary>Respuesta 201 de <c>POST /api/users</c> — nunca incluye la contraseña temporal.</summary>
public sealed record CreateUserResponse(Guid Id);
