using Procofa.Application.Abstractions.Audits;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Audits.ReplaceAuditPrograms;

public sealed class ReplaceAuditProgramsCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditRepository auditRepository,
    IProgramRepository programRepository,
    IAuditChecklistRepository auditChecklistRepository)
{
    public Task<ReplaceAuditProgramsResult> HandleAsync(
        ReplaceAuditProgramsCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var audit = await auditRepository.GetByIdAsync(tenantId, command.AuditId, ct);
                if (audit is null)
                {
                    return ReplaceAuditProgramsResult.Failure(ReplaceAuditProgramsError.NotFound);
                }

                if (!audit.IsEditable)
                {
                    return ReplaceAuditProgramsResult.Failure(ReplaceAuditProgramsError.NotEditable);
                }

                var programCodes = (command.ProgramCodes ?? []).Distinct().ToArray();
                var resolvedPrograms = await programRepository.FindManyByCodesAsync(programCodes, ct);
                if (resolvedPrograms.Count != programCodes.Length)
                {
                    var missing = programCodes.Except(resolvedPrograms.Select(p => p.Code));
                    return ReplaceAuditProgramsResult.Failure(
                        ReplaceAuditProgramsError.ProgramNotFound,
                        $"Programa(s) no encontrados en el catálogo: {string.Join(", ", missing)}.");
                }

                var newProgramIds = resolvedPrograms.Select(p => p.Id).ToHashSet();
                var assignedChecklists = await auditChecklistRepository.ListDetailedByAuditAsync(
                    tenantId, audit.Id, ct);
                var orphaned = assignedChecklists.FirstOrDefault(d => !newProgramIds.Contains(d.ProgramId));
                if (orphaned is not null)
                {
                    return ReplaceAuditProgramsResult.Failure(
                        ReplaceAuditProgramsError.ChecklistOrphaned,
                        $"Checklist ya asignado ({orphaned.ChecklistName}) depende de un programa que dejaría " +
                        "de estar en la auditoría.");
                }

                audit.ReplacePrograms(resolvedPrograms.Select(p => p.Id).ToArray());

                return ReplaceAuditProgramsResult.Success();
            },
            cancellationToken);
    }
}
