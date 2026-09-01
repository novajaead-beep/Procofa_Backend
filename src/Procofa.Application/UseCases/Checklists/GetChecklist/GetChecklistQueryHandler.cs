using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Checklists.GetChecklist;

public sealed class GetChecklistQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistRepository checklistRepository)
{
    public Task<GetChecklistResult> HandleAsync(GetChecklistQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var checklist = await checklistRepository.GetByIdAsync(tenantId, query.ChecklistId, ct);
                if (checklist is null)
                {
                    return GetChecklistResult.NotFound();
                }

                return GetChecklistResult.Success(
                    checklist.Id, checklist.ProgramId, checklist.ProfileId, checklist.AuditTypeId, checklist.Name,
                    checklist.Description, checklist.IsActive, checklist.CreatedAtUtc, checklist.UpdatedAtUtc);
            },
            cancellationToken);
    }
}
