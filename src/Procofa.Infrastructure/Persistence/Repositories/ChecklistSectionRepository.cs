using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class ChecklistSectionRepository(ProcofaDbContext dbContext) : IChecklistSectionRepository
{
    public Task<ChecklistSection?> GetByIdAsync(
        Guid tenantId, Guid checklistVersionId, Guid sectionId, CancellationToken cancellationToken) =>
        dbContext.ChecklistSections.FirstOrDefaultAsync(
            s => s.TenantId == tenantId && s.ChecklistVersionId == checklistVersionId && s.Id == sectionId,
            cancellationToken);

    public Task AddAsync(ChecklistSection section, CancellationToken cancellationToken)
    {
        dbContext.ChecklistSections.Add(section);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(ChecklistSection section, CancellationToken cancellationToken)
    {
        dbContext.ChecklistSections.Remove(section);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ChecklistSection>> ListByVersionAsync(
        Guid tenantId, Guid checklistVersionId, CancellationToken cancellationToken) =>
        await dbContext.ChecklistSections.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ChecklistVersionId == checklistVersionId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyForVersionAsync(
        Guid tenantId, Guid checklistVersionId, CancellationToken cancellationToken) =>
        dbContext.ChecklistSections.AnyAsync(
            s => s.TenantId == tenantId && s.ChecklistVersionId == checklistVersionId, cancellationToken);
}
