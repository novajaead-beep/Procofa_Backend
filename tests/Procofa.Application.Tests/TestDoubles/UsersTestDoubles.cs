using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Clients;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Tests.TestDoubles;

/// <summary>Fakes deterministas para los tests de gestión de usuarios — mismo criterio que <see
/// cref="InMemoryRoleCatalog"/> y compañía en AuthTestDoubles.cs: sin librería de
/// mocking.</summary>
internal sealed class FakeCurrentUser(Guid userId, params string[] roles) : ICurrentUser
{
    public Guid UserId { get; } = userId;
    public IReadOnlyCollection<string> Roles { get; } = roles;
}

internal sealed class FakeClientRepository : IClientRepository
{
    private readonly List<Client> _clients = [];

    public FakeClientRepository(params Client[] seedClients) => _clients.AddRange(seedClients);

    public IReadOnlyList<Client> Clients => _clients;

    public Task<IReadOnlyCollection<Client>> FindManyByIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Client>>(
            _clients.Where(c => c.TenantId == tenantId && clientIds.Contains(c.Id)).ToArray());

    public Task<Client?> GetByIdAsync(Guid tenantId, Guid clientId, CancellationToken cancellationToken) =>
        Task.FromResult(_clients.FirstOrDefault(c => c.TenantId == tenantId && c.Id == clientId));

    public Task AddAsync(Client client, CancellationToken cancellationToken)
    {
        _clients.Add(client);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByTaxIdAsync(
        Guid tenantId, string taxId, Guid? excludeClientId, CancellationToken cancellationToken) =>
        Task.FromResult(_clients.Any(c =>
            c.TenantId == tenantId && c.TaxId == taxId && c.Id != (excludeClientId ?? Guid.Empty)));

    public Task<ClientListPageResult> ListAsync(
        Guid tenantId, string? search, bool? isActive, string? programCode,
        IReadOnlyCollection<Guid>? restrictToClientIds, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        if (restrictToClientIds is not null && restrictToClientIds.Count == 0)
        {
            return Task.FromResult(new ClientListPageResult([], 0));
        }

        IEnumerable<Client> query = _clients.Where(c => c.TenantId == tenantId);

        if (restrictToClientIds is not null)
        {
            query = query.Where(c => restrictToClientIds.Contains(c.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.LegalName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (c.TradeName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (c.TaxId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(programCode))
        {
            query = query.Where(c => c.Programs.Any(p => InMemoryProgramCatalog.CodeById(p.ProgramId) == programCode));
        }

        var materialized = query.OrderBy(c => c.LegalName).ToList();
        var items = materialized
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClientListRow(
                c.Id, c.LegalName, c.TradeName, c.TaxId, c.IsActive,
                c.Programs.Select(p => InMemoryProgramCatalog.CodeById(p.ProgramId)).ToArray(), c.CreatedAtUtc))
            .ToList();

        return Task.FromResult(new ClientListPageResult(items, materialized.Count));
    }
}

internal static class InMemoryProgramCatalog
{
    public static readonly ComplianceProgram Oea = new(Guid.NewGuid(), "OEA", "Operador Económico Autorizado", null);
    public static readonly ComplianceProgram Ctpat = new(Guid.NewGuid(), "CTPAT", "C-TPAT", null);

    public static readonly IReadOnlyList<ComplianceProgram> All = [Oea, Ctpat];

    public static string CodeById(Guid id) => All.FirstOrDefault(p => p.Id == id)?.Code ?? id.ToString();
}

internal sealed class FakeProgramRepository : IProgramRepository
{
    public Task<IReadOnlyCollection<ComplianceProgram>> FindManyByCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<ComplianceProgram>>(
            InMemoryProgramCatalog.All.Where(p => codes.Contains(p.Code)).ToArray());

    public Task<IReadOnlyCollection<string>> GetCodesByIdsAsync(
        IReadOnlyCollection<Guid> programIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<string>>(
            InMemoryProgramCatalog.All.Where(p => programIds.Contains(p.Id)).Select(p => p.Code).ToArray());
}

internal sealed class FakeAuditedCompanyRepository : IAuditedCompanyRepository
{
    private readonly List<AuditedCompany> _companies = [];

    public FakeAuditedCompanyRepository(params AuditedCompany[] seedCompanies) => _companies.AddRange(seedCompanies);

    public IReadOnlyList<AuditedCompany> Companies => _companies;

    public Task<AuditedCompany?> GetByIdAsync(
        Guid tenantId, Guid clientId, Guid companyId, CancellationToken cancellationToken) =>
        Task.FromResult(_companies.FirstOrDefault(
            c => c.TenantId == tenantId && c.ClientId == clientId && c.Id == companyId));

    public Task AddAsync(AuditedCompany company, CancellationToken cancellationToken)
    {
        _companies.Add(company);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByTaxIdAsync(
        Guid tenantId, Guid clientId, string taxId, Guid? excludeCompanyId, CancellationToken cancellationToken) =>
        Task.FromResult(_companies.Any(c =>
            c.TenantId == tenantId && c.ClientId == clientId && c.TaxId == taxId &&
            c.Id != (excludeCompanyId ?? Guid.Empty)));

    public Task<AuditedCompanyListPageResult> ListAsync(
        Guid tenantId, Guid clientId, string? search, bool? isActive, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        IEnumerable<AuditedCompany> query = _companies.Where(c => c.TenantId == tenantId && c.ClientId == clientId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.LegalName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        var materialized = query.OrderBy(c => c.LegalName).ToList();
        var items = materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new AuditedCompanyListPageResult(items, materialized.Count));
    }

    public Task<IReadOnlyDictionary<Guid, int>> CountByClientIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken)
    {
        var counts = _companies
            .Where(c => c.TenantId == tenantId && clientIds.Contains(c.ClientId))
            .GroupBy(c => c.ClientId)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult<IReadOnlyDictionary<Guid, int>>(counts);
    }
}

internal sealed class FakeCompanySiteRepository : ICompanySiteRepository
{
    private readonly List<CompanySite> _sites = [];

    public FakeCompanySiteRepository(params CompanySite[] seedSites) => _sites.AddRange(seedSites);

    public IReadOnlyList<CompanySite> Sites => _sites;

    public Task<CompanySite?> GetByIdAsync(
        Guid tenantId, Guid companyId, Guid siteId, CancellationToken cancellationToken) =>
        Task.FromResult(_sites.FirstOrDefault(
            s => s.TenantId == tenantId && s.AuditedCompanyId == companyId && s.Id == siteId));

    public Task AddAsync(CompanySite site, CancellationToken cancellationToken)
    {
        _sites.Add(site);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CompanySite>> ListByCompanyAsync(
        Guid tenantId, Guid companyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<CompanySite>>(
            _sites.Where(s => s.TenantId == tenantId && s.AuditedCompanyId == companyId)
                .OrderBy(s => s.Name).ToList());
}

internal sealed class FakeClientContactRepository : IClientContactRepository
{
    private readonly List<ClientContact> _contacts = [];

    public FakeClientContactRepository(params ClientContact[] seedContacts) => _contacts.AddRange(seedContacts);

    public IReadOnlyList<ClientContact> Contacts => _contacts;

    public Task<ClientContact?> GetByIdAsync(
        Guid tenantId, Guid clientId, Guid contactId, CancellationToken cancellationToken) =>
        Task.FromResult(_contacts.FirstOrDefault(
            c => c.TenantId == tenantId && c.ClientId == clientId && c.Id == contactId));

    public Task AddAsync(ClientContact contact, CancellationToken cancellationToken)
    {
        _contacts.Add(contact);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ClientContact>> ListByClientAsync(
        Guid tenantId, Guid clientId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ClientContact>>(
            _contacts.Where(c => c.TenantId == tenantId && c.ClientId == clientId)
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToList());
}
