using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.ChecklistVersions.PublishChecklistVersion;

public sealed class PublishChecklistVersionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository,
    ICriterionRepository criterionRepository,
    IDateTimeProvider dateTimeProvider)
{
    public Task<PublishChecklistVersionResult> HandleAsync(
        PublishChecklistVersionCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, command.ChecklistId, command.VersionId, ct);
                if (version is null)
                {
                    return PublishChecklistVersionResult.Failure(PublishChecklistVersionError.NotFound);
                }

                if (!version.IsEditable)
                {
                    return PublishChecklistVersionResult.Failure(PublishChecklistVersionError.AlreadyPublished);
                }

                if (!await checklistSectionRepository.AnyForVersionAsync(tenantId, version.Id, ct))
                {
                    return PublishChecklistVersionResult.Failure(PublishChecklistVersionError.NoSections);
                }

                if (!await criterionRepository.AnyForVersionAsync(tenantId, version.Id, ct))
                {
                    return PublishChecklistVersionResult.Failure(PublishChecklistVersionError.NoCriteria);
                }

                version.Publish(dateTimeProvider.UtcNow);

                return PublishChecklistVersionResult.Success();
            },
            cancellationToken);
    }
}
