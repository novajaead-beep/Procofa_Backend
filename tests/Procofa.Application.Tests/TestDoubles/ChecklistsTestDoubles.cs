using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.TestDoubles;

internal static class InMemoryProfileCatalog
{
    public static readonly Profile Maquila = new(Guid.NewGuid(), "MAQUILA", "Maquiladora", null);
    public static readonly Profile Transportista = new(Guid.NewGuid(), "TRANSPORTISTA", "Transportista", null);

    public static readonly IReadOnlyList<Profile> All = [Maquila, Transportista];
}

internal sealed class FakeProfileRepository : IProfileRepository
{
    public Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(InMemoryProfileCatalog.All.FirstOrDefault(p => p.Id == id));

    public Task<Profile?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(InMemoryProfileCatalog.All.FirstOrDefault(
            p => string.Equals(p.Code, code, StringComparison.Ordinal)));
}

internal static class InMemoryAuditTypeCatalog
{
    public static readonly AuditType InternaOea = new(Guid.NewGuid(), "INTERNA_OEA", "Interna OEA", null);
    public static readonly AuditType InternaCtpat = new(Guid.NewGuid(), "INTERNA_CTPAT", "Interna C-TPAT", null);

    public static readonly IReadOnlyList<AuditType> All = [InternaOea, InternaCtpat];
}

internal sealed class FakeAuditTypeRepository : IAuditTypeRepository
{
    public Task<AuditType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(InMemoryAuditTypeCatalog.All.FirstOrDefault(a => a.Id == id));

    public Task<AuditType?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(InMemoryAuditTypeCatalog.All.FirstOrDefault(
            a => string.Equals(a.Code, code, StringComparison.Ordinal)));
}

internal sealed class FakeChecklistRepository : IChecklistRepository
{
    private readonly List<Checklist> _checklists = [];

    public FakeChecklistRepository(params Checklist[] seedChecklists) => _checklists.AddRange(seedChecklists);

    public IReadOnlyList<Checklist> Checklists => _checklists;

    public Task<Checklist?> GetByIdAsync(Guid tenantId, Guid checklistId, CancellationToken cancellationToken) =>
        Task.FromResult(_checklists.FirstOrDefault(c => c.TenantId == tenantId && c.Id == checklistId));

    public Task AddAsync(Checklist checklist, CancellationToken cancellationToken)
    {
        _checklists.Add(checklist);
        return Task.CompletedTask;
    }

    public Task<ChecklistListPageResult> ListAsync(
        Guid tenantId, string? search, Guid? programId, Guid? profileId, Guid? auditTypeId, bool? isActive,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        IEnumerable<Checklist> query = _checklists.Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (c.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
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

        var materialized = query.OrderBy(c => c.Name).ToList();
        var items = materialized
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ChecklistListRow(
                c.Id, c.ProgramId, c.ProfileId, c.AuditTypeId, c.Name, c.Description, c.IsActive, c.CreatedAtUtc))
            .ToList();

        return Task.FromResult(new ChecklistListPageResult(items, materialized.Count));
    }

    public Task<Checklist?> FindActiveForResolutionAsync(
        Guid tenantId, Guid programId, Guid profileId, Guid? auditTypeId, CancellationToken cancellationToken) =>
        Task.FromResult(_checklists
            .Where(c =>
                c.TenantId == tenantId && c.IsActive && c.ProgramId == programId && c.ProfileId == profileId &&
                c.AuditTypeId == auditTypeId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenByDescending(c => c.Id)
            .FirstOrDefault());
}

internal sealed class FakeChecklistVersionRepository : IChecklistVersionRepository
{
    private readonly List<ChecklistVersion> _versions = [];

    public FakeChecklistVersionRepository(params ChecklistVersion[] seedVersions) => _versions.AddRange(seedVersions);

    public IReadOnlyList<ChecklistVersion> Versions => _versions;

    public Task<ChecklistVersion?> GetByIdAsync(
        Guid tenantId, Guid checklistId, Guid versionId, CancellationToken cancellationToken) =>
        Task.FromResult(_versions.FirstOrDefault(
            v => v.TenantId == tenantId && v.ChecklistId == checklistId && v.Id == versionId));

    public Task<IReadOnlyList<ChecklistVersion>> ListByChecklistAsync(
        Guid tenantId, Guid checklistId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChecklistVersion>>(_versions
            .Where(v => v.TenantId == tenantId && v.ChecklistId == checklistId)
            .OrderByDescending(v => v.VersionNumber)
            .ToArray());

    public Task<ChecklistVersion> CreateNextVersionAsync(
        Guid tenantId, Guid checklistId, Func<int, ChecklistVersion> factory, CancellationToken cancellationToken)
    {
        var maxVersion = _versions
            .Where(v => v.TenantId == tenantId && v.ChecklistId == checklistId)
            .Select(v => (int?)v.VersionNumber)
            .Max() ?? 0;

        var version = factory(maxVersion + 1);
        _versions.Add(version);
        return Task.FromResult(version);
    }

    public Task<ChecklistVersion?> GetLatestPublishedAsync(
        Guid tenantId, Guid checklistId, CancellationToken cancellationToken) =>
        Task.FromResult(_versions
            .Where(v => v.TenantId == tenantId && v.ChecklistId == checklistId &&
                        v.Status == ChecklistVersionStatus.Published)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault());
}

internal sealed class FakeChecklistSectionRepository : IChecklistSectionRepository
{
    private readonly List<ChecklistSection> _sections = [];

    public FakeChecklistSectionRepository(params ChecklistSection[] seedSections) => _sections.AddRange(seedSections);

    public IReadOnlyList<ChecklistSection> Sections => _sections;

    public Task<ChecklistSection?> GetByIdAsync(
        Guid tenantId, Guid checklistVersionId, Guid sectionId, CancellationToken cancellationToken) =>
        Task.FromResult(_sections.FirstOrDefault(
            s => s.TenantId == tenantId && s.ChecklistVersionId == checklistVersionId && s.Id == sectionId));

    public Task AddAsync(ChecklistSection section, CancellationToken cancellationToken)
    {
        _sections.Add(section);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(ChecklistSection section, CancellationToken cancellationToken)
    {
        _sections.Remove(section);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChecklistSection>> ListByVersionAsync(
        Guid tenantId, Guid checklistVersionId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChecklistSection>>(_sections
            .Where(s => s.TenantId == tenantId && s.ChecklistVersionId == checklistVersionId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Id)
            .ToArray());

    public Task<bool> AnyForVersionAsync(
        Guid tenantId, Guid checklistVersionId, CancellationToken cancellationToken) =>
        Task.FromResult(_sections.Any(s => s.TenantId == tenantId && s.ChecklistVersionId == checklistVersionId));
}

internal sealed class FakeCriterionRepository : ICriterionRepository
{
    private readonly List<Criterion> _criteria = [];
    private readonly FakeChecklistSectionRepository _sections;

    public FakeCriterionRepository(FakeChecklistSectionRepository sections, params Criterion[] seedCriteria)
    {
        _sections = sections;
        _criteria.AddRange(seedCriteria);
    }

    public IReadOnlyList<Criterion> Criteria => _criteria;

    public Task<Criterion?> GetByIdAsync(
        Guid tenantId, Guid checklistSectionId, Guid criterionId, CancellationToken cancellationToken) =>
        Task.FromResult(_criteria.FirstOrDefault(
            c => c.TenantId == tenantId && c.ChecklistSectionId == checklistSectionId && c.Id == criterionId));

    public Task AddAsync(Criterion criterion, CancellationToken cancellationToken)
    {
        _criteria.Add(criterion);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Criterion criterion, CancellationToken cancellationToken)
    {
        _criteria.Remove(criterion);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Criterion>> ListBySectionAsync(
        Guid tenantId, Guid checklistSectionId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Criterion>>(_criteria
            .Where(c => c.TenantId == tenantId && c.ChecklistSectionId == checklistSectionId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .ToArray());

    public Task<bool> ExistsByCodeAsync(
        Guid tenantId, Guid checklistSectionId, string code, Guid? excludeCriterionId,
        CancellationToken cancellationToken) =>
        Task.FromResult(_criteria.Any(c =>
            c.TenantId == tenantId && c.ChecklistSectionId == checklistSectionId && c.Code == code &&
            c.Id != (excludeCriterionId ?? Guid.Empty)));

    public Task<bool> ExistsForSectionAsync(
        Guid tenantId, Guid checklistSectionId, CancellationToken cancellationToken) =>
        Task.FromResult(_criteria.Any(c => c.TenantId == tenantId && c.ChecklistSectionId == checklistSectionId));

    public Task<bool> AnyForVersionAsync(
        Guid tenantId, Guid checklistVersionId, CancellationToken cancellationToken)
    {
        var sectionIds = _sections.Sections
            .Where(s => s.TenantId == tenantId && s.ChecklistVersionId == checklistVersionId)
            .Select(s => s.Id)
            .ToHashSet();

        return Task.FromResult(_criteria.Any(c => c.TenantId == tenantId && sectionIds.Contains(c.ChecklistSectionId)));
    }
}
