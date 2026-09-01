using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.ChecklistVersions.ListChecklistVersions;

public sealed class ListChecklistVersionsQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistRepository checklistRepository,
    IChecklistVersionRepository checklistVersionRepository)
{
    public Task<ListChecklistVersionsResult> HandleAsync(
        ListChecklistVersionsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var checklist = await checklistRepository.GetByIdAsync(tenantId, query.ChecklistId, ct);
                if (checklist is null)
                {
                    return ListChecklistVersionsResult.Failure(ListChecklistVersionsError.ChecklistNotFound);
                }

                var versions = await checklistVersionRepository.ListByChecklistAsync(tenantId, query.ChecklistId, ct);

                var items = versions
                    .Select(v => new ChecklistVersionListItem(
                        v.Id, v.VersionNumber, v.Status.ToString().ToUpperInvariant(), v.PublishedAtUtc,
                        v.CreatedAtUtc))
                    .ToArray();

                return ListChecklistVersionsResult.Success(items);
            },
            cancellationToken);
    }
}
