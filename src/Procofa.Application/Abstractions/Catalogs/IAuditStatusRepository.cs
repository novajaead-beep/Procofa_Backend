using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Application.Abstractions.Catalogs;

/// <summary>Puerto de solo-lectura sobre el catálogo <see cref="AuditStatus"/> (tabla física
/// <c>audit_statuses</c>). Catálogo global sin <c>tenant_id</c>.</summary>
public interface IAuditStatusRepository
{
    Task<AuditStatus?> FindByCodeAsync(string code, CancellationToken cancellationToken);

    Task<AuditStatus?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
