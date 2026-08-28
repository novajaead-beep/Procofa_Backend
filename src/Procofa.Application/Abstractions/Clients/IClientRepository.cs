using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Abstractions.Clients;

/// <summary>Fila proyectada de <c>GET /api/clients</c> — ya incluye los códigos de programa
/// asignados. La cantidad de empresas auditadas se resuelve aparte (<see
/// cref="IAuditedCompanyRepository.CountByClientIdsAsync"/>), en una sola consulta agrupada por
/// página — evita mezclar dos aggregates distintos (Client, AuditedCompany) en una sola
/// query.</summary>
public sealed record ClientListRow(
    Guid Id,
    string LegalName,
    string? TradeName,
    string? TaxId,
    bool IsActive,
    IReadOnlyCollection<string> ProgramCodes,
    DateTime CreatedAtUtc);

/// <summary>Página de resultados de <c>GET /api/clients</c>.</summary>
public sealed record ClientListPageResult(IReadOnlyList<ClientListRow> Items, int Total);

/// <summary>
/// Puerto de acceso a <see cref="Client"/>. Deliberadamente NO es un repositorio genérico.
/// </summary>
public interface IClientRepository
{
    /// <summary>
    /// Resuelve varios clientes de una vez por id, filtrando SIEMPRE por
    /// tenant — un <c>clientId</c> de otro tenant simplemente no aparece en
    /// el resultado (nunca revela su existencia). El caller compara la
    /// cuenta contra los ids solicitados para detectar ids inexistentes o de
    /// otro tenant.
    /// </summary>
    Task<IReadOnlyCollection<Client>> FindManyByIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken);

    /// <summary><c>GET /api/clients/{id}</c> y los endpoints de escritura: carga el cliente CON
    /// <see cref="Client.Programs"/> ya incluido. <c>null</c> si no existe dentro del tenant.
    /// </summary>
    Task<Client?> GetByIdAsync(Guid tenantId, Guid clientId, CancellationToken cancellationToken);

    Task AddAsync(Client client, CancellationToken cancellationToken);

    /// <summary><c>POST/PUT /api/clients</c>: unicidad de <c>tax_id</c> dentro del tenant (índice
    /// parcial <c>uq_clients_tenant_tax_id</c> — solo aplica si <paramref name="taxId"/> no es
    /// null). <paramref name="excludeClientId"/> permite excluir al propio cliente en un
    /// update.</summary>
    Task<bool> ExistsByTaxIdAsync(
        Guid tenantId, string taxId, Guid? excludeClientId, CancellationToken cancellationToken);

    /// <summary><c>GET /api/clients</c>: listado paginado del tenant actual. <paramref
    /// name="search"/> busca en legal_name/trade_name/tax_id; <paramref name="programCode"/> filtra
    /// por programa asignado; <paramref name="restrictToClientIds"/> no-nulo acota el resultado a
    /// ese conjunto (alcance de lectura de CLIENTE vía <c>user_client_access</c> — un conjunto
    /// vacío produce una página vacía, nunca se interpreta como "sin restricción").</summary>
    Task<ClientListPageResult> ListAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        string? programCode,
        IReadOnlyCollection<Guid>? restrictToClientIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
