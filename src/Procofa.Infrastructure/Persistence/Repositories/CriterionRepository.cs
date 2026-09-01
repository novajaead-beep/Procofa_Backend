using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class CriterionRepository(ProcofaDbContext dbContext) : ICriterionRepository
{
    public Task<Criterion?> GetByIdAsync(
        Guid tenantId, Guid checklistSectionId, Guid criterionId, CancellationToken cancellationToken) =>
        dbContext.Criteria.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.ChecklistSectionId == checklistSectionId && c.Id == criterionId,
            cancellationToken);

    public Task AddAsync(Criterion criterion, CancellationToken cancellationToken)
    {
        dbContext.Criteria.Add(criterion);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Criterion criterion, CancellationToken cancellationToken)
    {
        dbContext.Criteria.Remove(criterion);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Criterion>> ListBySectionAsync(
        Guid tenantId, Guid checklistSectionId, CancellationToken cancellationToken) =>
        await dbContext.Criteria.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.ChecklistSectionId == checklistSectionId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsByCodeAsync(
        Guid tenantId, Guid checklistSectionId, string code, Guid? excludeCriterionId,
        CancellationToken cancellationToken) =>
        dbContext.Criteria.AnyAsync(
            c => c.TenantId == tenantId && c.ChecklistSectionId == checklistSectionId && c.Code == code &&
                 c.Id != (excludeCriterionId ?? Guid.Empty),
            cancellationToken);

    public Task<bool> ExistsForSectionAsync(
        Guid tenantId, Guid checklistSectionId, CancellationToken cancellationToken) =>
        dbContext.Criteria.AnyAsync(
            c => c.TenantId == tenantId && c.ChecklistSectionId == checklistSectionId, cancellationToken);

    public Task<bool> AnyForVersionAsync(
        Guid tenantId, Guid checklistVersionId, CancellationToken cancellationToken) =>
        dbContext.Criteria.AnyAsync(
            c => c.TenantId == tenantId &&
                 dbContext.ChecklistSections.Any(s =>
                     s.Id == c.ChecklistSectionId && s.ChecklistVersionId == checklistVersionId),
            cancellationToken);
}
