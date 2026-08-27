using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// Puerto de acceso a <see cref="User"/> para el flujo de Auth (Instrucción
/// 04). Deliberadamente NO es un repositorio genérico (<c>IRepository&lt;T&gt;</c>
/// prohibido explícitamente): expone únicamente las operaciones que Login y
/// el bootstrap del primer ADMIN necesitan, nada de CRUD completo (eso es
/// alcance de una instrucción futura de gestión de usuarios).
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
}
