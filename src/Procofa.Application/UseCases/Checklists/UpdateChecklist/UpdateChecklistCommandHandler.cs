using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Checklists.UpdateChecklist;

public sealed class UpdateChecklistCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistRepository checklistRepository,
    IProgramRepository programRepository,
    IProfileRepository profileRepository,
    IAuditTypeRepository auditTypeRepository)
{
    public Task<UpdateChecklistResult> HandleAsync(UpdateChecklistCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Task.FromResult(
                UpdateChecklistResult.Failure(UpdateChecklistError.ValidationFailed, "name es obligatorio."));
        }

        if (command.ProgramId is null || command.ProfileId is null)
        {
            return Task.FromResult(UpdateChecklistResult.Failure(
                UpdateChecklistError.ValidationFailed, "programId y profileId son obligatorios."));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<UpdateChecklistResult> ExecuteAsync(
        Guid tenantId, UpdateChecklistCommand command, CancellationToken ct)
    {
        var checklist = await checklistRepository.GetByIdAsync(tenantId, command.ChecklistId, ct);
        if (checklist is null)
        {
            return UpdateChecklistResult.Failure(UpdateChecklistError.NotFound);
        }

        if (await programRepository.GetByIdAsync(command.ProgramId!.Value, ct) is null)
        {
            return UpdateChecklistResult.Failure(UpdateChecklistError.ProgramNotFound);
        }

        if (await profileRepository.GetByIdAsync(command.ProfileId!.Value, ct) is null)
        {
            return UpdateChecklistResult.Failure(UpdateChecklistError.ProfileNotFound);
        }

        if (command.AuditTypeId.HasValue &&
            await auditTypeRepository.GetByIdAsync(command.AuditTypeId.Value, ct) is null)
        {
            return UpdateChecklistResult.Failure(UpdateChecklistError.AuditTypeNotFound);
        }

        checklist.UpdateDetails(
            command.ProgramId.Value, command.ProfileId.Value, command.AuditTypeId, command.Name!,
            command.Description);

        return UpdateChecklistResult.Success();
    }
}
