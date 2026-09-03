using Procofa.Application.Abstractions.Audits;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.TestDoubles;

internal static class InMemoryAuditStatusCatalog
{
    public static readonly AuditStatus Borrador = new(Guid.NewGuid(), "BORRADOR", "Borrador", 1, false);
    public static readonly AuditStatus Programada = new(Guid.NewGuid(), "PROGRAMADA", "Programada", 2, false);

    public static readonly IReadOnlyList<AuditStatus> All = [Borrador, Programada];
}

internal sealed class FakeAuditStatusRepository : IAuditStatusRepository
{
    public Task<AuditStatus?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(InMemoryAuditStatusCatalog.All.FirstOrDefault(
            s => string.Equals(s.Code, code, StringComparison.Ordinal)));

    public Task<AuditStatus?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(InMemoryAuditStatusCatalog.All.FirstOrDefault(s => s.Id == id));
}

internal sealed class FakeAuditRepository : IAuditRepository
{
    private readonly List<Audit> _audits = [];

    public FakeAuditRepository(params Audit[] seedAudits) => _audits.AddRange(seedAudits);

    public IReadOnlyList<Audit> Audits => _audits;

    public Task<Audit?> GetByIdAsync(Guid tenantId, Guid auditId, CancellationToken cancellationToken) =>
        Task.FromResult(_audits.FirstOrDefault(a => a.TenantId == tenantId && a.Id == auditId));

    public Task AddAsync(Audit audit, CancellationToken cancellationToken)
    {
        _audits.Add(audit);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsFolioAsync(Guid tenantId, string folio, CancellationToken cancellationToken) =>
        Task.FromResult(_audits.Any(a => a.TenantId == tenantId && a.Folio == folio));

    public Task<AuditListPageResult> ListAsync(
        Guid tenantId, Guid? clientId, Guid? companyId, string? status, Guid? auditTypeId, string? executionMode,
        string? search, int page, int pageSize, IReadOnlyCollection<Guid>? clientScope,
        CancellationToken cancellationToken)
    {
        if (clientScope is not null && clientScope.Count == 0)
        {
            return Task.FromResult(new AuditListPageResult([], 0));
        }

        IEnumerable<Audit> query = _audits.Where(a => a.TenantId == tenantId);

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
            var statusId = InMemoryAuditStatusCatalog.All.FirstOrDefault(s => s.Code == status)?.Id;
            query = statusId is null ? [] : query.Where(a => a.StatusId == statusId.Value);
        }

        if (!string.IsNullOrWhiteSpace(executionMode))
        {
            query = query.Where(a => ExecutionModeToRequestString(a.ExecutionMode) == executionMode);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.Folio.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                a.Objective.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var materialized = query.OrderByDescending(a => a.ScheduledDate).ToList();
        var items = materialized
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditListRow(
                a.Id, a.Folio, a.ClientId, a.AuditedCompanyId, a.CompanySiteId, a.AuditTypeId, a.ProfileId,
                a.StatusId, a.Objective, a.ScheduledDate, a.StartedAtUtc, ExecutionModeToRequestString(a.ExecutionMode),
                a.CreatedAtUtc))
            .ToList();

        return Task.FromResult(new AuditListPageResult(items, materialized.Count));
    }

    private static string ExecutionModeToRequestString(ExecutionMode executionMode) => executionMode switch
    {
        ExecutionMode.Onsite => "ONSITE",
        ExecutionMode.Remote => "REMOTE",
        ExecutionMode.Hybrid => "HYBRID",
        _ => throw new ArgumentOutOfRangeException(nameof(executionMode), executionMode, null),
    };
}

internal sealed class FakeAuditChecklistRepository : IAuditChecklistRepository
{
    private readonly List<AuditChecklist> _auditChecklists = [];
    private readonly FakeChecklistRepository? _checklists;
    private readonly FakeChecklistVersionRepository? _versions;

    public FakeAuditChecklistRepository() { }

    /// <summary>Habilita <see cref="ListDetailedByAuditAsync"/> — resuelve el join en memoria contra
    /// los mismos fakes de Checklist/ChecklistVersion usados por el resto del fixture, mismo patrón
    /// que <c>FakeCriterionRepository</c> correlacionándose con <c>FakeChecklistSectionRepository</c>.
    /// </summary>
    public FakeAuditChecklistRepository(FakeChecklistRepository checklists, FakeChecklistVersionRepository versions)
    {
        _checklists = checklists;
        _versions = versions;
    }

    public IReadOnlyList<AuditChecklist> AuditChecklists => _auditChecklists;

    public Task<IReadOnlyList<AuditChecklist>> ListByAuditAsync(
        Guid tenantId, Guid auditId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AuditChecklist>>(
            _auditChecklists.Where(ac => ac.TenantId == tenantId && ac.AuditId == auditId).ToArray());

    public Task<IReadOnlyList<AuditChecklistDetail>> ListDetailedByAuditAsync(
        Guid tenantId, Guid auditId, CancellationToken cancellationToken)
    {
        if (_checklists is null || _versions is null)
        {
            throw new InvalidOperationException(
                "FakeAuditChecklistRepository: usa el constructor con FakeChecklistRepository/" +
                "FakeChecklistVersionRepository para resolver ListDetailedByAuditAsync.");
        }

        var details = from ac in _auditChecklists
                      where ac.TenantId == tenantId && ac.AuditId == auditId
                      join version in _versions.Versions on new { ac.TenantId, ChecklistVersionId = ac.ChecklistVersionId }
                          equals new { version.TenantId, ChecklistVersionId = version.Id }
                      join checklist in _checklists.Checklists on new { version.TenantId, ChecklistId = version.ChecklistId }
                          equals new { checklist.TenantId, ChecklistId = checklist.Id }
                      select new AuditChecklistDetail(
                          ac.Id, checklist.Id, version.Id, version.VersionNumber, checklist.Name, checklist.ProgramId,
                          checklist.ProfileId, checklist.AuditTypeId, ac.AssignedAtUtc);

        return Task.FromResult<IReadOnlyList<AuditChecklistDetail>>(
            details.ToArray());
    }

    public Task ReplaceAsync(
        Guid tenantId, Guid auditId, IReadOnlyCollection<AuditChecklist> newChecklists,
        CancellationToken cancellationToken)
    {
        _auditChecklists.RemoveAll(ac => ac.TenantId == tenantId && ac.AuditId == auditId);
        _auditChecklists.AddRange(newChecklists);
        return Task.CompletedTask;
    }
}
