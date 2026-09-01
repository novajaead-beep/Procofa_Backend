using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.UseCases.ChecklistVersions.CreateChecklistVersion;

/// <summary>Siempre crea una versión vacía — no existe semántica de clonado especificada; una
/// versión copiando la estructura de otra queda para una instrucción futura si se define.</summary>
public sealed class CreateChecklistVersionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistRepository checklistRepository,
    IChecklistVersionRepository checklistVersionRepository,
    ICurrentUser currentUser)
{
    public Task<CreateChecklistVersionResult> HandleAsync(
        CreateChecklistVersionCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<CreateChecklistVersionResult> ExecuteAsync(
        Guid tenantId, CreateChecklistVersionCommand command, CancellationToken ct)
    {
        var checklist = await checklistRepository.GetByIdAsync(tenantId, command.ChecklistId, ct);
        if (checklist is null)
        {
            return CreateChecklistVersionResult.Failure(CreateChecklistVersionError.ChecklistNotFound);
        }

        var version = await checklistVersionRepository.CreateNextVersionAsync(
            tenantId,
            command.ChecklistId,
            versionNumber =>
            {
                var created = new ChecklistVersion(
                    Guid.NewGuid(), tenantId, command.ChecklistId, versionNumber, currentUser.UserId);

                if (!string.IsNullOrWhiteSpace(command.ChangeNotes))
                {
                    created.UpdateDetails(command.ChangeNotes);
                }

                return created;
            },
            ct);

        return CreateChecklistVersionResult.Success(version.Id, version.VersionNumber);
    }
}
