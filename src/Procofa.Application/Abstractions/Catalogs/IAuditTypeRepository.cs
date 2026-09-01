using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Application.Abstractions.Catalogs;

/// <summary>Puerto de solo-lectura sobre el catálogo <see cref="AuditType"/> (tabla física
/// <c>audit_types</c>). Catálogo global sin <c>tenant_id</c>.</summary>
public interface IAuditTypeRepository
{
    Task<AuditType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AuditType?> FindByCodeAsync(string code, CancellationToken cancellationToken);
}
