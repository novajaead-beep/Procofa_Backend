using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.ChecklistVersions.UpdateChecklistVersion;

public sealed class UpdateChecklistVersionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository)
{
    public Task<UpdateChecklistVersionResult> HandleAsync(
        UpdateChecklistVersionCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, command.ChecklistId, command.VersionId, ct);
                if (version is null)
                {
                    return UpdateChecklistVersionResult.Failure(UpdateChecklistVersionError.NotFound);
                }

                if (!version.IsEditable)
                {
                    return UpdateChecklistVersionResult.Failure(UpdateChecklistVersionError.VersionPublished);
                }

                version.UpdateDetails(command.ChangeNotes);

                return UpdateChecklistVersionResult.Success();
            },
            cancellationToken);
    }
}
