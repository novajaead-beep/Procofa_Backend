using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Abstractions.Clients;

/// <summary> Puerto de solo-lectura sobre <see cref="Client"/>mínimo para (gestión de acceso de
/// usuarios CLIENTE vía <c>user_client_access</c>). Deliberadamente NO es CRUD de clientes (fuera
/// de alcance — sección "NO implementar todavía": "gestión de clientes/empresas/sedes") ni un
/// repositorio genérico. </summary>
public interface IClientRepository
{
    /// <summary>
    /// Resuelve varios clientes de una vez por id, filtrando SIEMPRE por
    /// tenant — un <c>clientId</c> de otro tenant simplemente no aparece en
    /// el resultado (nunca revela su existencia). El caller compara la
    /// cuenta contra los ids solicitados para detectar ids inexistentes o de
    /// otro tenant.
    /// </summary>
    Task<IReadOnlyCollection<Client>> FindManyByIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken);
}
