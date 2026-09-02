using Procofa.Application.Abstractions.Audits;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Audits.UpdateAudit;

/// <summary>Caso de uso <c>PUT /api/audits/{auditId}</c>. Toda precondición de negocio
/// (editable, execution_mode↔sede) se valida ANTES de invocar <c>Audit.UpdateDetails</c> — el
/// aggregate nunca llega a lanzar en el flujo normal del handler; sus checks internos
/// (<c>EnsureEditable</c>/<c>EnsureExecutionModeMatchesSite</c>) son defensa en profundidad, mismo
/// criterio que <c>UpdateChecklistVersionCommandHandler</c> con <c>ChecklistVersion.IsEditable</c>.
/// </summary>
public sealed class UpdateAuditCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditRepository auditRepository,
    IAuditedCompanyRepository auditedCompanyRepository,
    ICompanySiteRepository companySiteRepository,
    IAuditTypeRepository auditTypeRepository,
    IProfileRepository profileRepository,
    IAuditChecklistRepository auditChecklistRepository)
{
    public Task<UpdateAuditResult> HandleAsync(UpdateAuditCommand command, CancellationToken cancellationToken)
    {
        var validationError = Validate(command, out var executionMode);
        if (validationError is not null)
        {
            return Task.FromResult(UpdateAuditResult.Failure(UpdateAuditError.ValidationFailed, validationError));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, executionMode, ct), cancellationToken);
    }

    private async Task<UpdateAuditResult> ExecuteAsync(
        Guid tenantId, UpdateAuditCommand command, Domain.Enums.ExecutionMode executionMode, CancellationToken ct)
    {
        var audit = await auditRepository.GetByIdAsync(tenantId, command.AuditId, ct);
        if (audit is null)
        {
            return UpdateAuditResult.Failure(UpdateAuditError.NotFound);
        }

        if (!audit.IsEditable)
        {
            return UpdateAuditResult.Failure(UpdateAuditError.NotEditable);
        }

        if (await auditedCompanyRepository.GetByIdAsync(
                tenantId, audit.ClientId, command.AuditedCompanyId!.Value, ct) is null)
        {
            return UpdateAuditResult.Failure(UpdateAuditError.AuditedCompanyNotFound);
        }

        if (command.CompanySiteId.HasValue &&
            await companySiteRepository.GetByIdAsync(
                tenantId, command.AuditedCompanyId.Value, command.CompanySiteId.Value, ct) is null)
        {
            return UpdateAuditResult.Failure(UpdateAuditError.CompanySiteNotFound);
        }

        if (await auditTypeRepository.GetByIdAsync(command.AuditTypeId!.Value, ct) is null)
        {
            return UpdateAuditResult.Failure(UpdateAuditError.AuditTypeNotFound);
        }

        if (await profileRepository.GetByIdAsync(command.ProfileId!.Value, ct) is null)
        {
            return UpdateAuditResult.Failure(UpdateAuditError.ProfileNotFound);
        }

        if (command.ProfileId.Value != audit.ProfileId || command.AuditTypeId.Value != audit.AuditTypeId)
        {
            var incompatibleDetail = await FindIncompatibleAssignedChecklistAsync(
                tenantId, audit, command.ProfileId.Value, command.AuditTypeId.Value, ct);
            if (incompatibleDetail is not null)
            {
                return UpdateAuditResult.Failure(
                    UpdateAuditError.ChecklistIncompatible,
                    $"Checklist ya asignado ({incompatibleDetail.ChecklistName}) deja de ser compatible con " +
                    "el nuevo profile/audit_type.");
            }
        }

        audit.UpdateDetails(
            command.AuditedCompanyId.Value, command.CompanySiteId, command.AuditTypeId.Value,
            command.ProfileId.Value, command.Objective!, command.Scope!, command.Methodology,
            command.ScheduledDate!.Value, executionMode);

        return UpdateAuditResult.Success();
    }

    /// <summary>Un <c>AuditChecklist</c> ya asignado congela una <c>ChecklistVersion</c> concreta —
    /// si el cambio de Profile/AuditType deja al <c>Checklist</c> detrás de esa versión fuera de
    /// compatibilidad (mismo criterio que valida <c>ReplaceAuditChecklistsCommandHandler</c> al
    /// asociar), <c>UpdateAudit</c> debe rechazarse en vez de dejar el historial inconsistente. Los
    /// programas no cambian en <c>UpdateAudit</c>, así que <c>ProgramId</c> del checklist ya
    /// asignado sigue siendo válido por construcción — se revalida igual por completitud.</summary>
    private async Task<AuditChecklistDetail?> FindIncompatibleAssignedChecklistAsync(
        Guid tenantId, Domain.Entities.Audits.Audit audit, Guid newProfileId, Guid newAuditTypeId,
        CancellationToken ct)
    {
        var assignedProgramIds = audit.Programs.Select(p => p.ProgramId).ToHashSet();
        var assignedChecklists = await auditChecklistRepository.ListDetailedByAuditAsync(tenantId, audit.Id, ct);

        return assignedChecklists.FirstOrDefault(detail =>
            detail.ProfileId != newProfileId ||
            (detail.AuditTypeId is not null && detail.AuditTypeId != newAuditTypeId) ||
            !assignedProgramIds.Contains(detail.ProgramId));
    }

    private static string? Validate(UpdateAuditCommand command, out Domain.Enums.ExecutionMode executionMode)
    {
        executionMode = default;

        if (command.AuditedCompanyId is null)
        {
            return "auditedCompanyId es obligatorio.";
        }

        if (command.AuditTypeId is null)
        {
            return "auditTypeId es obligatorio.";
        }

        if (command.ProfileId is null)
        {
            return "profileId es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.Objective))
        {
            return "objective es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.Scope))
        {
            return "scope es obligatorio.";
        }

        if (command.ScheduledDate is null)
        {
            return "scheduledDate es obligatorio.";
        }

        if (!ExecutionModeParser.TryParse(command.ExecutionMode, out executionMode))
        {
            return "executionMode debe ser ONSITE, REMOTE o HYBRID.";
        }

        if (ExecutionModeParser.RequiresCompanySite(executionMode) && command.CompanySiteId is null)
        {
            return $"execution_mode = {command.ExecutionMode} requiere companySiteId.";
        }

        return null;
    }
}
