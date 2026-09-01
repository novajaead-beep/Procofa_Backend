using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Criteria.GetCriterion;

public sealed class GetCriterionQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository,
    ICriterionRepository criterionRepository)
{
    public Task<GetCriterionResult> HandleAsync(GetCriterionQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, query.ChecklistId, query.VersionId, ct);
                if (version is null)
                {
                    return GetCriterionResult.NotFound();
                }

                var section = await checklistSectionRepository.GetByIdAsync(
                    tenantId, query.VersionId, query.SectionId, ct);
                if (section is null)
                {
                    return GetCriterionResult.NotFound();
                }

                var criterion = await criterionRepository.GetByIdAsync(tenantId, section.Id, query.CriterionId, ct);
                if (criterion is null)
                {
                    return GetCriterionResult.NotFound();
                }

                return GetCriterionResult.Success(
                    criterion.Id, criterion.Code, criterion.AuditQuestion, criterion.AuditorInterpretation,
                    criterion.ExpectedEvidence, criterion.ExpectedEvidenceType, criterion.ImportanceLevel,
                    criterion.NormativeReference, criterion.EvaluationRecommendation, criterion.IsMandatory,
                    criterion.SortOrder);
            },
            cancellationToken);
    }
}
