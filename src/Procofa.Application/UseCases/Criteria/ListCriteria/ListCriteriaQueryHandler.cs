using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Criteria.ListCriteria;

public sealed class ListCriteriaQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository,
    ICriterionRepository criterionRepository)
{
    public Task<ListCriteriaResult> HandleAsync(ListCriteriaQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, query.ChecklistId, query.VersionId, ct);
                if (version is null)
                {
                    return ListCriteriaResult.Failure(ListCriteriaError.SectionNotFound);
                }

                var section = await checklistSectionRepository.GetByIdAsync(
                    tenantId, query.VersionId, query.SectionId, ct);
                if (section is null)
                {
                    return ListCriteriaResult.Failure(ListCriteriaError.SectionNotFound);
                }

                var criteria = await criterionRepository.ListBySectionAsync(tenantId, section.Id, ct);

                var items = criteria
                    .Select(c => new CriterionListItem(c.Id, c.Code, c.AuditQuestion, c.IsMandatory, c.SortOrder))
                    .ToArray();

                return ListCriteriaResult.Success(items);
            },
            cancellationToken);
    }
}
