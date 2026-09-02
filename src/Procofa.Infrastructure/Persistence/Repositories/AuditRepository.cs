using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Audits;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación de <see cref="IAuditRepository"/>. Toda query asume que ya corre dentro de la
/// transacción tenant-scoped abierta por <c>ITenantUnitOfWork</c> — RLS filtra por tenant a nivel
/// de BD; el filtro explícito <c>TenantId == tenantId</c> es defensa en profundidad.
/// </summary>
public sealed class AuditRepository(ProcofaDbContext dbContext) : IAuditRepository
{
    public Task<Audit?> GetByIdAsync(Guid tenantId, Guid auditId, CancellationToken cancellationToken) =>
        dbContext.Audits
            .Include(a => a.Programs)
            .Include(a => a.Team)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == auditId, cancellationToken);

    public Task AddAsync(Audit audit, CancellationToken cancellationToken)
    {
        dbContext.Audits.Add(audit);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsFolioAsync(Guid tenantId, string folio, CancellationToken cancellationToken) =>
        dbContext.Audits.AnyAsync(a => a.TenantId == tenantId && a.Folio == folio, cancellationToken);

    public async Task<AuditListPageResult> ListAsync(
        Guid tenantId,
        Guid? clientId,
        Guid? companyId,
        string? status,
        Guid? auditTypeId,
        string? executionMode,
        string? search,
        int page,
        int pageSize,
        IReadOnlyCollection<Guid>? clientScope,
        CancellationToken cancellationToken)
    {
        if (clientScope is not null && clientScope.Count == 0)
        {
            return new AuditListPageResult([], 0);
        }

        var query = dbContext.Audits.AsNoTracking().Where(a => a.TenantId == tenantId);

        if (clientScope is not null)
        {
            query = query.Where(a => clientScope.Contains(a.ClientId));
        }

        if (clientId.HasValue)
        {
            query = query.Where(a => a.ClientId == clientId.Value);
        }

        if (companyId.HasValue)
        {
            query = query.Where(a => a.AuditedCompanyId == companyId.Value);
        }

        if (auditTypeId.HasValue)
        {
            query = query.Where(a => a.AuditTypeId == auditTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => dbContext.AuditStatuses.Any(s => s.Id == a.StatusId && s.Code == status));
        }

        if (TryParseExecutionMode(executionMode, out var parsedExecutionMode))
        {
            query = query.Where(a => a.ExecutionMode == parsedExecutionMode);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.Folio, pattern) || EF.Functions.ILike(a.Objective, pattern));
        }

        var total = await query.CountAsync(cancellationToken);

        // ToRequestString no es traducible a SQL (método C# arbitrario) — se materializa primero la
        // entidad y se convierte ExecutionMode a string en memoria, no dentro del Select() de EF.
        var pageRows = await query
            .OrderByDescending(a => a.ScheduledDate)
            .ThenByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = pageRows
            .Select(a => new AuditListRow(
                a.Id, a.Folio, a.ClientId, a.AuditedCompanyId, a.CompanySiteId, a.AuditTypeId, a.ProfileId,
                a.StatusId, a.Objective, a.ScheduledDate, a.StartedAtUtc, ToRequestString(a.ExecutionMode),
                a.CreatedAtUtc))
            .ToList();

        return new AuditListPageResult(items, total);
    }

    private static bool TryParseExecutionMode(string? value, out ExecutionMode executionMode)
    {
        switch (value)
        {
            case "ONSITE":
                executionMode = ExecutionMode.Onsite;
                return true;
            case "REMOTE":
                executionMode = ExecutionMode.Remote;
                return true;
            case "HYBRID":
                executionMode = ExecutionMode.Hybrid;
                return true;
            default:
                executionMode = default;
                return false;
        }
    }

    private static string ToRequestString(ExecutionMode executionMode) => executionMode switch
    {
        ExecutionMode.Onsite => "ONSITE",
        ExecutionMode.Remote => "REMOTE",
        ExecutionMode.Hybrid => "HYBRID",
        _ => throw new ArgumentOutOfRangeException(nameof(executionMode), executionMode, null),
    };
}
