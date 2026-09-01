using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Abstractions.Checklists;

public interface IChecklistVersionRepository
{
    Task<ChecklistVersion?> GetByIdAsync(
        Guid tenantId, Guid checklistId, Guid versionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChecklistVersion>> ListByChecklistAsync(
        Guid tenantId, Guid checklistId, CancellationToken cancellationToken);

    /// <summary>Asigna <c>version_number</c> de forma segura frente a altas concurrentes para el
    /// mismo checklist y agrega la versión construida por <paramref name="factory"/>. La
    /// implementación serializa por <paramref name="checklistId"/> dentro de la transacción vigente
    /// — nunca <c>MAX(version_number)+1</c> sin protección.</summary>
    Task<ChecklistVersion> CreateNextVersionAsync(
        Guid tenantId, Guid checklistId, Func<int, ChecklistVersion> factory, CancellationToken cancellationToken);

    /// <summary>Última versión con estado <c>PUBLISHED</c> del checklist, resuelta
    /// determinísticamente por <c>version_number</c> descendente — usada por la resolución de
    /// plantilla (<c>GET /api/checklists/resolve</c>), que nunca debe devolver una versión
    /// DRAFT.</summary>
    Task<ChecklistVersion?> GetLatestPublishedAsync(
        Guid tenantId, Guid checklistId, CancellationToken cancellationToken);
}
