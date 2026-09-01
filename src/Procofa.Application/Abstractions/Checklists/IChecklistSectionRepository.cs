using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Abstractions.Checklists;

public interface IChecklistSectionRepository
{
    Task<ChecklistSection?> GetByIdAsync(
        Guid tenantId, Guid checklistVersionId, Guid sectionId, CancellationToken cancellationToken);

    Task AddAsync(ChecklistSection section, CancellationToken cancellationToken);

    Task RemoveAsync(ChecklistSection section, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChecklistSection>> ListByVersionAsync(
        Guid tenantId, Guid checklistVersionId, CancellationToken cancellationToken);

    Task<bool> AnyForVersionAsync(Guid tenantId, Guid checklistVersionId, CancellationToken cancellationToken);
}
