using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Abstractions.Clients;

/// <summary>Puerto de acceso a <see cref="ClientContact"/>.</summary>
public interface IClientContactRepository
{
    /// <summary>Carga el contacto validando que pertenezca a <paramref name="clientId"/> dentro
    /// del tenant — <c>null</c> si no existe o pertenece a otro client/tenant.</summary>
    Task<ClientContact?> GetByIdAsync(
        Guid tenantId, Guid clientId, Guid contactId, CancellationToken cancellationToken);

    Task AddAsync(ClientContact contact, CancellationToken cancellationToken);

    /// <summary>Todos los contactos activos e inactivos del cliente, ordenados por apellido/nombre
    /// — sin paginación, dado el volumen acotado de contactos por cliente.</summary>
    Task<IReadOnlyList<ClientContact>> ListByClientAsync(
        Guid tenantId, Guid clientId, CancellationToken cancellationToken);
}
