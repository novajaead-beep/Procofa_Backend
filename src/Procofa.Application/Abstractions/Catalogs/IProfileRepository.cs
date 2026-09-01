using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Application.Abstractions.Catalogs;

/// <summary>Puerto de solo-lectura sobre el catálogo <see cref="Profile"/> (tabla física
/// <c>profiles</c>). Catálogo global sin <c>tenant_id</c>.</summary>
public interface IProfileRepository
{
    Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Profile?> FindByCodeAsync(string code, CancellationToken cancellationToken);
}
