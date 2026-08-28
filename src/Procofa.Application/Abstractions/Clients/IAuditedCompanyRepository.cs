using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Abstractions.Clients;

public sealed record AuditedCompanyListPageResult(IReadOnlyList<AuditedCompany> Items, int Total);

/// <summary>Puerto de acceso a <see cref="AuditedCompany"/>.</summary>
public interface IAuditedCompanyRepository
{
    /// <summary>Carga la empresa auditada validando que pertenezca a <paramref name="clientId"/>
    /// dentro del tenant — <c>null</c> si no existe o pertenece a otro client/tenant (nunca revela
    /// cuál de los dos).</summary>
    Task<AuditedCompany?> GetByIdAsync(
        Guid tenantId, Guid clientId, Guid companyId, CancellationToken cancellationToken);

    Task AddAsync(AuditedCompany company, CancellationToken cancellationToken);

    Task<bool> ExistsByTaxIdAsync(
        Guid tenantId, Guid clientId, string taxId, Guid? excludeCompanyId, CancellationToken cancellationToken);

    Task<AuditedCompanyListPageResult> ListAsync(
        Guid tenantId, Guid clientId, string? search, bool? isActive, int page, int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Cuenta de empresas auditadas por cliente, para <see
    /// cref="ClientListRow.AuditedCompanyCount"/> — una sola consulta agrupada por página de
    /// clientes, nunca una consulta por cliente.</summary>
    Task<IReadOnlyDictionary<Guid, int>> CountByClientIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken);
}
