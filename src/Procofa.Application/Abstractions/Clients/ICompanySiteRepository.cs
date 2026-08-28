using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Abstractions.Clients;

/// <summary>Puerto de acceso a <see cref="CompanySite"/>.</summary>
public interface ICompanySiteRepository
{
    /// <summary>Carga el sitio validando que pertenezca a <paramref name="companyId"/> dentro del
    /// tenant — <c>null</c> si no existe o pertenece a otra empresa/tenant.</summary>
    Task<CompanySite?> GetByIdAsync(
        Guid tenantId, Guid companyId, Guid siteId, CancellationToken cancellationToken);

    Task AddAsync(CompanySite site, CancellationToken cancellationToken);

    /// <summary>Todos los sitios activos e inactivos de la empresa, ordenados por nombre — sin
    /// paginación, dado el volumen acotado de sedes por empresa auditada.</summary>
    Task<IReadOnlyList<CompanySite>> ListByCompanyAsync(
        Guid tenantId, Guid companyId, CancellationToken cancellationToken);
}
