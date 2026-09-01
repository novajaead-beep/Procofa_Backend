using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.UseCases.Criteria.CreateCriterion;

public sealed class CreateCriterionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository,
    ICriterionRepository criterionRepository)
{
    public Task<CreateCriterionResult> HandleAsync(CreateCriterionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.AuditQuestion))
        {
            return Task.FromResult(CreateCriterionResult.Failure(CreateCriterionError.ValidationFailed));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, command.ChecklistId, command.VersionId, ct);
                if (version is null)
                {
                    return CreateCriterionResult.Failure(CreateCriterionError.SectionNotFound);
                }

                var section = await checklistSectionRepository.GetByIdAsync(
                    tenantId, command.VersionId, command.SectionId, ct);
                if (section is null)
                {
                    return CreateCriterionResult.Failure(CreateCriterionError.SectionNotFound);
                }

                if (!version.IsEditable)
                {
                    return CreateCriterionResult.Failure(CreateCriterionError.VersionPublished);
                }

                if (await criterionRepository.ExistsByCodeAsync(
                        tenantId, section.Id, command.Code!, excludeCriterionId: null, ct))
                {
                    return CreateCriterionResult.Failure(CreateCriterionError.CodeAlreadyExists);
                }

                var criterion = new Criterion(
                    Guid.NewGuid(), tenantId, section.Id, command.Code!, command.AuditQuestion!,
                    command.AuditorInterpretation, command.ExpectedEvidence, command.ExpectedEvidenceType,
                    command.ImportanceLevel, command.NormativeReference, command.EvaluationRecommendation,
                    command.IsMandatory, command.SortOrder);

                await criterionRepository.AddAsync(criterion, ct);

                return CreateCriterionResult.Success(criterion.Id);
            },
            cancellationToken);
    }
}
