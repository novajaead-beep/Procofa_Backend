using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// Puerto de solo-lectura sobre el catálogo <see cref="Role"/>. Identidad semántica estable por
/// <see cref="Role.Code"/> — nunca se hardcodea el UUID de un rol en código (decisión congelada #5,
/// baseline V2.1), ver la nota de <see cref="Role"/>. </summary>
public interface IRoleRepository
{
    Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>
    /// resuelve varios roles de una vez por su <see cref="Role.Code"/> (asignación de roles al
    /// crear/editar un usuario) — evita N consultas individuales. Códigos que no existen en el
    /// catálogo simplemente no aparecen en el resultado; el caller compara la cuenta para detectar
    /// códigos inválidos. </summary>
    Task<IReadOnlyCollection<Role>> FindManyByCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken cancellationToken);
}
