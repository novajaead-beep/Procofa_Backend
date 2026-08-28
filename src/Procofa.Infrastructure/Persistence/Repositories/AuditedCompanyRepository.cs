using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Clients;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class AuditedCompanyRepository(ProcofaDbContext dbContext) : IAuditedCompanyRepository
{
    // Tracked (nunca AsNoTracking): los casos de uso de escritura mutan la entidad devuelta y
    // confían en que ITenantUnitOfWork.ExecuteWriteAsync haga SaveChanges sobre esos cambios.
    public Task<AuditedCompany?> GetByIdAsync(
        Guid tenantId, Guid clientId, Guid companyId, CancellationToken cancellationToken) =>
        dbContext.AuditedCompanies
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.ClientId == clientId && c.Id == companyId, cancellationToken);

    public Task AddAsync(AuditedCompany company, CancellationToken cancellationToken)
    {
        dbContext.AuditedCompanies.Add(company);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByTaxIdAsync(
        Guid tenantId, Guid clientId, string taxId, Guid? excludeCompanyId, CancellationToken cancellationToken) =>
        dbContext.AuditedCompanies.AnyAsync(
            c => c.TenantId == tenantId && c.ClientId == clientId && c.TaxId == taxId &&
                c.Id != (excludeCompanyId ?? Guid.Empty),
            cancellationToken);

    public async Task<AuditedCompanyListPageResult> ListAsync(
        Guid tenantId, Guid clientId, string? search, bool? isActive, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AuditedCompanies.Where(c => c.TenantId == tenantId && c.ClientId == clientId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.LegalName, pattern) ||
                (c.TradeName != null && EF.Functions.ILike(c.TradeName, pattern)) ||
                (c.TaxId != null && EF.Functions.ILike(c.TaxId, pattern)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .AsNoTracking()
            .OrderBy(c => c.LegalName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new AuditedCompanyListPageResult(items, total);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountByClientIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await dbContext.AuditedCompanies
            .Where(c => c.TenantId == tenantId && clientIds.Contains(c.ClientId))
            .GroupBy(c => c.ClientId)
            .Select(g => new { ClientId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ClientId, x => x.Count, cancellationToken);
    }
}
