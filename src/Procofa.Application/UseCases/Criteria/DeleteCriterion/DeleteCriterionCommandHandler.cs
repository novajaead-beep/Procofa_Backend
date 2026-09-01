using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Criteria.DeleteCriterion;

public sealed class DeleteCriterionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository,
    ICriterionRepository criterionRepository)
{
    public Task<DeleteCriterionResult> HandleAsync(DeleteCriterionCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, command.ChecklistId, command.VersionId, ct);
                if (version is null)
                {
                    return DeleteCriterionResult.Failure(DeleteCriterionError.NotFound);
                }

                var section = await checklistSectionRepository.GetByIdAsync(
                    tenantId, command.VersionId, command.SectionId, ct);
                if (section is null)
                {
                    return DeleteCriterionResult.Failure(DeleteCriterionError.NotFound);
                }

                var criterion = await criterionRepository.GetByIdAsync(tenantId, section.Id, command.CriterionId, ct);
                if (criterion is null)
                {
                    return DeleteCriterionResult.Failure(DeleteCriterionError.NotFound);
                }

                if (!version.IsEditable)
                {
                    return DeleteCriterionResult.Failure(DeleteCriterionError.VersionPublished);
                }

                await criterionRepository.RemoveAsync(criterion, ct);

                return DeleteCriterionResult.Success();
            },
            cancellationToken);
    }
}
