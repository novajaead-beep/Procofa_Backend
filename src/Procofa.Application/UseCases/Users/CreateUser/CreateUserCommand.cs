namespace Procofa.Application.UseCases.Users.CreateUser;

/// <summary><c>POST /api/users</c>. <see cref="Roles"/> y <see cref="ClientIds"/> llegan tal como
/// el cliente los envió — TODA validación (mínimo un rol, catálogo permitido, clientIds solo con
/// rol CLIENTE, existencia/tenant de cada cliente) ocurre dentro del handler, nunca antes.
/// </summary>
public sealed record CreateUserCommand(
    string? Email,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? TemporaryPassword,
    IReadOnlyCollection<string>? Roles,
    IReadOnlyCollection<Guid>? ClientIds);
