using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Abstractions.Checklists;

public interface ICriterionRepository
{
    Task<Criterion?> GetByIdAsync(
        Guid tenantId, Guid checklistSectionId, Guid criterionId, CancellationToken cancellationToken);

    Task AddAsync(Criterion criterion, CancellationToken cancellationToken);

    Task RemoveAsync(Criterion criterion, CancellationToken cancellationToken);

    Task<IReadOnlyList<Criterion>> ListBySectionAsync(
        Guid tenantId, Guid checklistSectionId, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(
        Guid tenantId, Guid checklistSectionId, string code, Guid? excludeCriterionId,
        CancellationToken cancellationToken);

    Task<bool> ExistsForSectionAsync(Guid tenantId, Guid checklistSectionId, CancellationToken cancellationToken);

    /// <summary>Al menos un criterion en cualquier sección de la versión — usado por la validación
    /// de publicación ("tiene al menos un criterio utilizable").</summary>
    Task<bool> AnyForVersionAsync(Guid tenantId, Guid checklistVersionId, CancellationToken cancellationToken);
}
