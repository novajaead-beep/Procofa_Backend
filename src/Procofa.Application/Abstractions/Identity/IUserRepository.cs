using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// Fila proyectada de <c>GET /api/users</c> (Instrucción 05) — ya incluye los
/// códigos de rol resueltos, para que Application no tenga que hacer un
/// segundo viaje por usuario. Deliberadamente NO expone <c>password_hash</c>.
/// </summary>
public sealed record UserListRow(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    bool IsActive,
    bool MustChangePassword,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAtUtc);

/// <summary>Página de resultados de <c>GET /api/users</c> (Instrucción 05) — el total es el conteo SIN paginar, para construir la respuesta <c>{ items, page, pageSize, total }</c>.</summary>
public sealed record UserListPageResult(IReadOnlyList<UserListRow> Items, int Total);

/// <summary>
/// Puerto de acceso a <see cref="User"/> (Instrucción 04: Auth; ampliado en
/// Instrucción 05: gestión de usuarios). Deliberadamente NO es un
/// repositorio genérico (<c>IRepository&lt;T&gt;</c> prohibido explícitamente):
/// expone únicamente las operaciones que Auth y la administración de
/// usuarios necesitan.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Busca un usuario por email normalizado dentro del tenant, con
    /// <see cref="User.Roles"/> cargado (necesario para resolver los
    /// códigos de rol del JWT). Debe ejecutarse dentro de la transacción
    /// tenant-scoped abierta por <c>ITenantUnitOfWork</c> — nunca por fuera.
    /// </summary>
    Task<User?> FindByNormalizedEmailAsync(
        Guid tenantId, string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Resuelve los códigos (<see cref="Role.Code"/>) de un conjunto de <c>role_id</c>.</summary>
    Task<IReadOnlyCollection<string>> GetRoleCodesAsync(
        IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken);

    /// <summary>Agrega un nuevo usuario — usado únicamente por el bootstrap del primer ADMIN.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken);

    /// <summary>
    /// True si ya existe, dentro del tenant, al menos un usuario con el rol
    /// indicado — usado por el bootstrap para ser idempotente ("Solo debe
    /// funcionar si no existe ningún usuario ADMIN inicial").
    /// </summary>
    Task<bool> ExistsWithRoleAsync(Guid tenantId, string roleCode, CancellationToken cancellationToken);

    /// <summary>Instrucción 05, <c>POST /api/users</c>: unicidad de email dentro del tenant (usa la misma normalización que <see cref="User.Normalize"/>).</summary>
    Task<bool> ExistsByNormalizedEmailAsync(Guid tenantId, string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Instrucción 05, <c>GET /api/users/{id}</c> y los endpoints de
    /// escritura (status/roles/client-access): carga el usuario CON
    /// <see cref="User.Roles"/> y <see cref="User.ClientAccess"/> ya
    /// incluidos (la implementación decide cómo — ej. <c>AsSplitQuery</c>
    /// para evitar el warning de múltiples colecciones). <c>null</c> si no
    /// existe dentro del tenant.
    /// </summary>
    Task<User?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Instrucción 05, <c>GET /api/users</c>: listado paginado del tenant
    /// actual. <paramref name="search"/> busca en email/first_name/last_name;
    /// <paramref name="roleCode"/>, si se provee, ya fue validado contra el
    /// catálogo permitido por el caller (Application) — aquí solo se aplica
    /// como filtro.
    /// </summary>
    Task<UserListPageResult> ListAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        string? roleCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
