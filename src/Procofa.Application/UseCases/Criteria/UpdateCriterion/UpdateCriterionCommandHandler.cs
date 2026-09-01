using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Criteria.UpdateCriterion;

public sealed class UpdateCriterionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository,
    ICriterionRepository criterionRepository)
{
    public Task<UpdateCriterionResult> HandleAsync(UpdateCriterionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.AuditQuestion))
        {
            return Task.FromResult(UpdateCriterionResult.Failure(UpdateCriterionError.ValidationFailed));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, command.ChecklistId, command.VersionId, ct);
                if (version is null)
                {
                    return UpdateCriterionResult.Failure(UpdateCriterionError.NotFound);
                }

                var section = await checklistSectionRepository.GetByIdAsync(
                    tenantId, command.VersionId, command.SectionId, ct);
                if (section is null)
                {
                    return UpdateCriterionResult.Failure(UpdateCriterionError.NotFound);
                }

                var criterion = await criterionRepository.GetByIdAsync(tenantId, section.Id, command.CriterionId, ct);
                if (criterion is null)
                {
                    return UpdateCriterionResult.Failure(UpdateCriterionError.NotFound);
                }

                if (!version.IsEditable)
                {
                    return UpdateCriterionResult.Failure(UpdateCriterionError.VersionPublished);
                }

                if (await criterionRepository.ExistsByCodeAsync(
                        tenantId, section.Id, command.Code!, criterion.Id, ct))
                {
                    return UpdateCriterionResult.Failure(UpdateCriterionError.CodeAlreadyExists);
                }

                criterion.UpdateDetails(
                    command.Code!, command.AuditQuestion!, command.AuditorInterpretation, command.ExpectedEvidence,
                    command.ExpectedEvidenceType, command.ImportanceLevel, command.NormativeReference,
                    command.EvaluationRecommendation, command.IsMandatory);
                criterion.ChangeOrder(command.SortOrder);

                return UpdateCriterionResult.Success();
            },
            cancellationToken);
    }
}
