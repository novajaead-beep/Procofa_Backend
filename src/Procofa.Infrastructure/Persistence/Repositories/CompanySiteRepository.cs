using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Clients;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class CompanySiteRepository(ProcofaDbContext dbContext) : ICompanySiteRepository
{
    public Task<CompanySite?> GetByIdAsync(
        Guid tenantId, Guid companyId, Guid siteId, CancellationToken cancellationToken) =>
        dbContext.CompanySites
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId && s.AuditedCompanyId == companyId && s.Id == siteId, cancellationToken);

    public Task AddAsync(CompanySite site, CancellationToken cancellationToken)
    {
        dbContext.CompanySites.Add(site);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<CompanySite>> ListByCompanyAsync(
        Guid tenantId, Guid companyId, CancellationToken cancellationToken) =>
        await dbContext.CompanySites
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.AuditedCompanyId == companyId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
}
