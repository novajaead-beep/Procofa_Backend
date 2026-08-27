using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// Puerto de solo-lectura sobre el catálogo <see cref="Role"/> (Instrucción
/// 04). Identidad semántica estable por <see cref="Role.Code"/> — nunca se
/// hardcodea el UUID de un rol en código (decisión congelada #5, baseline
/// V2.1), ver la nota de <see cref="Role"/>.
/// </summary>
public interface IRoleRepository
{
    Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken);
}
