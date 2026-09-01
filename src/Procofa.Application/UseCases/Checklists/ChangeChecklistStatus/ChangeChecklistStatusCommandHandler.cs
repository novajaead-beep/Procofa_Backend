using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Checklists.ChangeChecklistStatus;

public sealed class ChangeChecklistStatusCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistRepository checklistRepository)
{
    public Task<ChangeChecklistStatusResult> HandleAsync(
        ChangeChecklistStatusCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var checklist = await checklistRepository.GetByIdAsync(tenantId, command.ChecklistId, ct);
                if (checklist is null)
                {
                    return ChangeChecklistStatusResult.Failure(ChangeChecklistStatusError.NotFound);
                }

                if (command.IsActive)
                {
                    checklist.Activate();
                }
                else
                {
                    checklist.Deactivate();
                }

                return ChangeChecklistStatusResult.Success();
            },
            cancellationToken);
    }
}
