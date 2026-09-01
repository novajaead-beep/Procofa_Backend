using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.ChecklistVersions.GetChecklistVersion;

public sealed class GetChecklistVersionQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository)
{
    public Task<GetChecklistVersionResult> HandleAsync(
        GetChecklistVersionQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, query.ChecklistId, query.VersionId, ct);
                if (version is null)
                {
                    return GetChecklistVersionResult.NotFound();
                }

                return GetChecklistVersionResult.Success(
                    version.Id, version.VersionNumber, version.Status.ToString().ToUpperInvariant(),
                    version.ChangeNotes, version.PublishedAtUtc, version.CreatedAtUtc, version.UpdatedAtUtc);
            },
            cancellationToken);
    }
}
