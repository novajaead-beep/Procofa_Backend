using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class ChecklistVersionRepository(ProcofaDbContext dbContext) : IChecklistVersionRepository
{
    public Task<ChecklistVersion?> GetByIdAsync(
        Guid tenantId, Guid checklistId, Guid versionId, CancellationToken cancellationToken) =>
        dbContext.ChecklistVersions.FirstOrDefaultAsync(
            v => v.TenantId == tenantId && v.ChecklistId == checklistId && v.Id == versionId, cancellationToken);

    public async Task<IReadOnlyList<ChecklistVersion>> ListByChecklistAsync(
        Guid tenantId, Guid checklistId, CancellationToken cancellationToken) =>
        await dbContext.ChecklistVersions.AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.ChecklistId == checklistId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

    public async Task<ChecklistVersion> CreateNextVersionAsync(
        Guid tenantId, Guid checklistId, Func<int, ChecklistVersion> factory, CancellationToken cancellationToken)
    {
        // pg_advisory_xact_lock serializa por checklist_id dentro de la transacción vigente y se
        // libera automáticamente al commit/rollback — evita que dos altas concurrentes calculen el
        // mismo version_number a partir de un MAX() leído antes de que la otra confirme.
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({checklistId.ToString()}))", cancellationToken);

        var maxVersion = await dbContext.ChecklistVersions
            .Where(v => v.TenantId == tenantId && v.ChecklistId == checklistId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;

        var version = factory(maxVersion + 1);
        dbContext.ChecklistVersions.Add(version);

        return version;
    }

    public Task<ChecklistVersion?> GetLatestPublishedAsync(
        Guid tenantId, Guid checklistId, CancellationToken cancellationToken) =>
        dbContext.ChecklistVersions.AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.ChecklistId == checklistId &&
                        v.Status == ChecklistVersionStatus.Published)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);
}
