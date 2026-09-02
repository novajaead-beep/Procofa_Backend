using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class ChecklistRepository(ProcofaDbContext dbContext) : IChecklistRepository
{
    public Task<Checklist?> GetByIdAsync(Guid tenantId, Guid checklistId, CancellationToken cancellationToken) =>
        dbContext.Checklists.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == checklistId, cancellationToken);

    public Task AddAsync(Checklist checklist, CancellationToken cancellationToken)
    {
        dbContext.Checklists.Add(checklist);
        return Task.CompletedTask;
    }

    public async Task<ChecklistListPageResult> ListAsync(
        Guid tenantId,
        string? search,
        Guid? programId,
        Guid? profileId,
        Guid? auditTypeId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Checklists.AsNoTracking().Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, pattern) ||
                (c.Description != null && EF.Functions.ILike(c.Description, pattern)));
        }

        if (programId.HasValue)
        {
            query = query.Where(c => c.ProgramId == programId.Value);
        }

        if (profileId.HasValue)
        {
            query = query.Where(c => c.ProfileId == profileId.Value);
        }

        if (auditTypeId.HasValue)
        {
            query = query.Where(c => c.AuditTypeId == auditTypeId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ChecklistListRow(
                c.Id, c.ProgramId, c.ProfileId, c.AuditTypeId, c.Name, c.Description, c.IsActive, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new ChecklistListPageResult(items, total);
    }

    public async Task<IReadOnlyList<Checklist>> ListActiveCandidatesAsync(
        Guid tenantId, Guid programId, Guid profileId, Guid? auditTypeId, CancellationToken cancellationToken)
    {
        var query = dbContext.Checklists.AsNoTracking().Where(c =>
            c.TenantId == tenantId && c.ProgramId == programId && c.ProfileId == profileId &&
            c.IsActive && c.AuditTypeId == auditTypeId);

        return await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);
    }
}
