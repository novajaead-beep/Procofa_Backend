using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Application.UseCases.Checklists.ResolveChecklist;

/// <summary>Resuelve la plantilla aplicable (checklist + versión publicada vigente) por Program +
/// Profile + AuditType opcional. Prioriza coincidencia exacta de <c>audit_type_id</c>; si no hay
/// match exacto, cae al checklist genérico (<c>audit_type_id IS NULL</c>). Nunca devuelve una
/// versión DRAFT — solo la <c>PUBLISHED</c> más reciente por <c>version_number</c>. Aún no crea
/// <c>audit_checklists</c>/<c>audit_criteria</c>: es solo resolución de lectura.</summary>
public sealed class ResolveChecklistQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistRepository checklistRepository,
    IChecklistVersionRepository checklistVersionRepository,
    IProgramRepository programRepository,
    IProfileRepository profileRepository,
    IAuditTypeRepository auditTypeRepository)
{
    public Task<ResolveChecklistResult> HandleAsync(ResolveChecklistQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Program) || string.IsNullOrWhiteSpace(query.Profile))
        {
            return Task.FromResult(ResolveChecklistResult.Failure(
                ResolveChecklistError.ValidationFailed, "program y profile son obligatorios."));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(ct => ExecuteAsync(tenantId, query, ct), cancellationToken);
    }

    private async Task<ResolveChecklistResult> ExecuteAsync(
        Guid tenantId, ResolveChecklistQuery query, CancellationToken ct)
    {
        var program = await ResolveProgramAsync(query.Program!, ct);
        if (program is null)
        {
            return ResolveChecklistResult.Failure(ResolveChecklistError.NotFound, "Program no encontrado.");
        }

        var profile = await ResolveProfileAsync(query.Profile!, ct);
        if (profile is null)
        {
            return ResolveChecklistResult.Failure(ResolveChecklistError.NotFound, "Profile no encontrado.");
        }

        Guid? auditTypeId = null;
        if (!string.IsNullOrWhiteSpace(query.AuditType))
        {
            var auditType = await ResolveAuditTypeAsync(query.AuditType, ct);
            if (auditType is null)
            {
                return ResolveChecklistResult.Failure(ResolveChecklistError.NotFound, "AuditType no encontrado.");
            }

            auditTypeId = auditType.Id;
        }

        var resolution = await ChecklistPublishedResolver.ResolveAsync(
            checklistRepository, checklistVersionRepository, tenantId, program.Id, profile.Id, auditTypeId, ct);

        if (resolution is null)
        {
            return ResolveChecklistResult.Failure(ResolveChecklistError.NotFound, "Ningún checklist aplicable.");
        }

        return ResolveChecklistResult.Success(
            resolution.Checklist.Id, resolution.Checklist.Name, resolution.Version.Id,
            resolution.Version.VersionNumber, resolution.IsExactMatch);
    }

    private async Task<ComplianceProgram?> ResolveProgramAsync(string codeOrId, CancellationToken ct) =>
        Guid.TryParse(codeOrId, out var id)
            ? await programRepository.GetByIdAsync(id, ct)
            : await programRepository.FindByCodeAsync(codeOrId, ct);

    private async Task<Profile?> ResolveProfileAsync(string codeOrId, CancellationToken ct) =>
        Guid.TryParse(codeOrId, out var id)
            ? await profileRepository.GetByIdAsync(id, ct)
            : await profileRepository.FindByCodeAsync(codeOrId, ct);

    private async Task<Domain.Entities.Catalogs.AuditType?> ResolveAuditTypeAsync(string codeOrId, CancellationToken ct) =>
        Guid.TryParse(codeOrId, out var id)
            ? await auditTypeRepository.GetByIdAsync(id, ct)
            : await auditTypeRepository.FindByCodeAsync(codeOrId, ct);
}
