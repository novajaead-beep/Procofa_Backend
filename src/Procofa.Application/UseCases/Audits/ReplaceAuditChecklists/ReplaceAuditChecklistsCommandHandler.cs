using Procofa.Application.Abstractions.Audits;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Checklists;
using Procofa.Domain.Entities.Audits;

namespace Procofa.Application.UseCases.Audits.ReplaceAuditChecklists;

/// <summary>Caso de uso <c>PUT /api/audits/{auditId}/checklists</c>. Compatibilidad de
/// <c>AuditTypeId</c>: un checklist con <c>AuditTypeId</c> no nulo que no coincide EXACTO con el
/// de la auditoría se rechaza (409). Un checklist con <c>AuditTypeId</c> nulo (genérico) solo es
/// aceptado como FALLBACK: si ya existe, para el mismo Program+Profile+AuditType de la auditoría,
/// un checklist con <c>AuditTypeId</c> exacto activo Y con versión PUBLISHED, se rechaza (409) —
/// el genérico nunca desplaza a un exacto aplicable, mismo criterio de prioridad que
/// <c>ResolveChecklistQueryHandler</c> usa para la resolución automática.</summary>
public sealed class ReplaceAuditChecklistsCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditRepository auditRepository,
    IChecklistRepository checklistRepository,
    IChecklistVersionRepository checklistVersionRepository,
    IAuditChecklistRepository auditChecklistRepository)
{
    public Task<ReplaceAuditChecklistsResult> HandleAsync(
        ReplaceAuditChecklistsCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var audit = await auditRepository.GetByIdAsync(tenantId, command.AuditId, ct);
                if (audit is null)
                {
                    return ReplaceAuditChecklistsResult.Failure(ReplaceAuditChecklistsError.NotFound);
                }

                if (!audit.IsEditable)
                {
                    return ReplaceAuditChecklistsResult.Failure(ReplaceAuditChecklistsError.NotEditable);
                }

                var auditProgramIds = audit.Programs.Select(p => p.ProgramId).ToHashSet();
                var checklistIds = (command.ChecklistIds ?? []).Distinct().ToArray();
                var newChecklists = new List<AuditChecklist>(checklistIds.Length);

                foreach (var checklistId in checklistIds)
                {
                    var checklist = await checklistRepository.GetByIdAsync(tenantId, checklistId, ct);
                    if (checklist is null)
                    {
                        return ReplaceAuditChecklistsResult.Failure(
                            ReplaceAuditChecklistsError.ChecklistNotFound, $"Checklist no encontrado: {checklistId}.");
                    }

                    if (!checklist.IsActive)
                    {
                        return ReplaceAuditChecklistsResult.Failure(
                            ReplaceAuditChecklistsError.IncompatibleChecklist,
                            $"Checklist {checklistId}: está inactivo.");
                    }

                    if (!auditProgramIds.Contains(checklist.ProgramId))
                    {
                        return ReplaceAuditChecklistsResult.Failure(
                            ReplaceAuditChecklistsError.IncompatibleChecklist,
                            $"Checklist {checklistId}: el programa no pertenece a la auditoría.");
                    }

                    if (checklist.ProfileId != audit.ProfileId)
                    {
                        return ReplaceAuditChecklistsResult.Failure(
                            ReplaceAuditChecklistsError.IncompatibleChecklist,
                            $"Checklist {checklistId}: el profile no coincide con el de la auditoría.");
                    }

                    if (checklist.AuditTypeId.HasValue && checklist.AuditTypeId != audit.AuditTypeId)
                    {
                        return ReplaceAuditChecklistsResult.Failure(
                            ReplaceAuditChecklistsError.IncompatibleChecklist,
                            $"Checklist {checklistId}: el audit_type no coincide con el de la auditoría.");
                    }

                    if (!checklist.AuditTypeId.HasValue)
                    {
                        var exactResolution = await ChecklistPublishedResolver.TryResolveExactAsync(
                            checklistRepository, checklistVersionRepository, tenantId, checklist.ProgramId,
                            checklist.ProfileId, audit.AuditTypeId, ct);
                        if (exactResolution is not null)
                        {
                            return ReplaceAuditChecklistsResult.Failure(
                                ReplaceAuditChecklistsError.IncompatibleChecklist,
                                $"Checklist {checklistId}: es genérico pero existe un checklist exacto " +
                                "publicado para este audit_type; el genérico solo aplica como fallback.");
                        }
                    }

                    var publishedVersion = await checklistVersionRepository.GetLatestPublishedAsync(
                        tenantId, checklistId, ct);
                    if (publishedVersion is null)
                    {
                        return ReplaceAuditChecklistsResult.Failure(
                            ReplaceAuditChecklistsError.NoPublishedVersion,
                            $"Checklist {checklistId}: no tiene ninguna versión PUBLISHED.");
                    }

                    newChecklists.Add(new AuditChecklist(Guid.NewGuid(), tenantId, audit.Id, publishedVersion.Id));
                }

                await auditChecklistRepository.ReplaceAsync(tenantId, audit.Id, newChecklists, ct);

                return ReplaceAuditChecklistsResult.Success();
            },
            cancellationToken);
    }
}
