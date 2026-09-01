using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.ChecklistSections.UpdateChecklistSection;

public sealed class UpdateChecklistSectionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository)
{
    public Task<UpdateChecklistSectionResult> HandleAsync(
        UpdateChecklistSectionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Task.FromResult(UpdateChecklistSectionResult.Failure(UpdateChecklistSectionError.ValidationFailed));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, command.ChecklistId, command.VersionId, ct);
                if (version is null)
                {
                    return UpdateChecklistSectionResult.Failure(UpdateChecklistSectionError.NotFound);
                }

                var section = await checklistSectionRepository.GetByIdAsync(
                    tenantId, command.VersionId, command.SectionId, ct);
                if (section is null)
                {
                    return UpdateChecklistSectionResult.Failure(UpdateChecklistSectionError.NotFound);
                }

                if (!version.IsEditable)
                {
                    return UpdateChecklistSectionResult.Failure(UpdateChecklistSectionError.VersionPublished);
                }

                section.UpdateDetails(command.Code, command.Name!, command.Description);
                section.ChangeOrder(command.SortOrder);

                return UpdateChecklistSectionResult.Success();
            },
            cancellationToken);
    }
}
