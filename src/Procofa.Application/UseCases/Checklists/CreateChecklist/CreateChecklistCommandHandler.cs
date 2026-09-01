using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.UseCases.Checklists.CreateChecklist;

public sealed class CreateChecklistCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistRepository checklistRepository,
    IProgramRepository programRepository,
    IProfileRepository profileRepository,
    IAuditTypeRepository auditTypeRepository,
    ICurrentUser currentUser)
{
    public Task<CreateChecklistResult> HandleAsync(CreateChecklistCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Task.FromResult(
                CreateChecklistResult.Failure(CreateChecklistError.ValidationFailed, "name es obligatorio."));
        }

        if (command.ProgramId is null || command.ProfileId is null)
        {
            return Task.FromResult(CreateChecklistResult.Failure(
                CreateChecklistError.ValidationFailed, "programId y profileId son obligatorios."));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<CreateChecklistResult> ExecuteAsync(
        Guid tenantId, CreateChecklistCommand command, CancellationToken ct)
    {
        if (await programRepository.GetByIdAsync(command.ProgramId!.Value, ct) is null)
        {
            return CreateChecklistResult.Failure(CreateChecklistError.ProgramNotFound);
        }

        if (await profileRepository.GetByIdAsync(command.ProfileId!.Value, ct) is null)
        {
            return CreateChecklistResult.Failure(CreateChecklistError.ProfileNotFound);
        }

        if (command.AuditTypeId.HasValue &&
            await auditTypeRepository.GetByIdAsync(command.AuditTypeId.Value, ct) is null)
        {
            return CreateChecklistResult.Failure(CreateChecklistError.AuditTypeNotFound);
        }

        var checklist = new Checklist(
            Guid.NewGuid(), tenantId, command.ProgramId.Value, command.ProfileId.Value, command.AuditTypeId,
            command.Name!, command.Description, currentUser.UserId);

        await checklistRepository.AddAsync(checklist, ct);

        return CreateChecklistResult.Success(checklist.Id);
    }
}
