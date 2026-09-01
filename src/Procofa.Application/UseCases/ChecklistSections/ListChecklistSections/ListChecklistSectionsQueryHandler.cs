using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.ChecklistSections.ListChecklistSections;

public sealed class ListChecklistSectionsQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository)
{
    public Task<ListChecklistSectionsResult> HandleAsync(
        ListChecklistSectionsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, query.ChecklistId, query.VersionId, ct);
                if (version is null)
                {
                    return ListChecklistSectionsResult.Failure(ListChecklistSectionsError.VersionNotFound);
                }

                var sections = await checklistSectionRepository.ListByVersionAsync(tenantId, query.VersionId, ct);

                var items = sections
                    .Select(s => new ChecklistSectionListItem(s.Id, s.Code, s.Name, s.Description, s.SortOrder))
                    .ToArray();

                return ListChecklistSectionsResult.Success(items);
            },
            cancellationToken);
    }
}
