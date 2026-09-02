using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Audits;
using Procofa.Domain.Entities.Audits;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class AuditChecklistRepository(ProcofaDbContext dbContext) : IAuditChecklistRepository
{
    public async Task<IReadOnlyList<AuditChecklist>> ListByAuditAsync(
        Guid tenantId, Guid auditId, CancellationToken cancellationToken) =>
        await dbContext.AuditChecklists.AsNoTracking()
            .Where(ac => ac.TenantId == tenantId && ac.AuditId == auditId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditChecklistDetail>> ListDetailedByAuditAsync(
        Guid tenantId, Guid auditId, CancellationToken cancellationToken) =>
        await (
            from ac in dbContext.AuditChecklists.AsNoTracking()
            where ac.TenantId == tenantId && ac.AuditId == auditId
            join version in dbContext.ChecklistVersions.AsNoTracking()
                on new { ac.TenantId, ChecklistVersionId = ac.ChecklistVersionId }
                equals new { version.TenantId, ChecklistVersionId = version.Id }
            join checklist in dbContext.Checklists.AsNoTracking()
                on new { version.TenantId, ChecklistId = version.ChecklistId }
                equals new { checklist.TenantId, ChecklistId = checklist.Id }
            select new AuditChecklistDetail(
                ac.Id, checklist.Id, version.Id, version.VersionNumber, checklist.Name, checklist.ProgramId,
                checklist.ProfileId, checklist.AuditTypeId, ac.AssignedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task ReplaceAsync(
        Guid tenantId, Guid auditId, IReadOnlyCollection<AuditChecklist> newChecklists,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.AuditChecklists
            .Where(ac => ac.TenantId == tenantId && ac.AuditId == auditId)
            .ToListAsync(cancellationToken);

        dbContext.AuditChecklists.RemoveRange(existing);

        // Flush explícito de los DELETE antes de los INSERT: si una checklist_version se
        // reasocia sin cambios, el índice único (audit_id, checklist_version_id) rechazaría el
        // INSERT si EF los agrupara en el orden por defecto sin el borrado ya confirmado.
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.AuditChecklists.AddRange(newChecklists);
    }
}
