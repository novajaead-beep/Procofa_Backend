using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Clients;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación de <see cref="IClientRepository"/>. Toda query asume que ya corre dentro de la
/// transacción tenant-scoped abierta por <c>ITenantUnitOfWork</c> — RLS filtra por tenant a nivel
/// de BD; el filtro explícito <c>TenantId == tenantId</c> es defensa en profundidad, no el único
/// mecanismo de aislamiento. </summary>
public sealed class ClientRepository(ProcofaDbContext dbContext) : IClientRepository
{
    public async Task<IReadOnlyCollection<Client>> FindManyByIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Clients
            .Where(c => c.TenantId == tenantId && clientIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<Client?> GetByIdAsync(Guid tenantId, Guid clientId, CancellationToken cancellationToken) =>
        dbContext.Clients
            .Include(c => c.Programs)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == clientId, cancellationToken);

    public Task AddAsync(Client client, CancellationToken cancellationToken)
    {
        dbContext.Clients.Add(client);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByTaxIdAsync(
        Guid tenantId, string taxId, Guid? excludeClientId, CancellationToken cancellationToken) =>
        dbContext.Clients.AnyAsync(
            c => c.TenantId == tenantId && c.TaxId == taxId && c.Id != (excludeClientId ?? Guid.Empty),
            cancellationToken);

    public async Task<ClientListPageResult> ListAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        string? programCode,
        IReadOnlyCollection<Guid>? restrictToClientIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (restrictToClientIds is not null && restrictToClientIds.Count == 0)
        {
            return new ClientListPageResult([], 0);
        }

        var query = dbContext.Clients.Where(c => c.TenantId == tenantId);

        if (restrictToClientIds is not null)
        {
            query = query.Where(c => restrictToClientIds.Contains(c.Id));
        }

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

        if (!string.IsNullOrWhiteSpace(programCode))
        {
            query = query.Where(c => c.Programs.Any(p =>
                dbContext.CompliancePrograms.Any(cp => cp.Id == p.ProgramId && cp.Code == programCode)));
        }

        var total = await query.CountAsync(cancellationToken);

        var pageRows = await query
            .OrderBy(c => c.LegalName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.LegalName,
                c.TradeName,
                c.TaxId,
                c.IsActive,
                c.CreatedAtUtc,
                ProgramIds = c.Programs.Select(p => p.ProgramId).ToList(),
            })
            .ToListAsync(cancellationToken);

        var allProgramIds = pageRows.SelectMany(c => c.ProgramIds).Distinct().ToArray();
        var codesByProgramId = await dbContext.CompliancePrograms
            .Where(p => allProgramIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Code, cancellationToken);

        var items = pageRows
            .Select(c => new ClientListRow(
                c.Id, c.LegalName, c.TradeName, c.TaxId, c.IsActive,
                c.ProgramIds.Select(id => codesByProgramId.GetValueOrDefault(id, id.ToString())).ToArray(),
                c.CreatedAtUtc))
            .ToList();

        return new ClientListPageResult(items, total);
    }
}
