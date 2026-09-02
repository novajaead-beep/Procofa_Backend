using Procofa.Domain.Entities.Audits;

namespace Procofa.Application.Abstractions.Audits;

public sealed record AuditListRow(
    Guid Id,
    string Folio,
    Guid ClientId,
    Guid AuditedCompanyId,
    Guid? CompanySiteId,
    Guid AuditTypeId,
    Guid ProfileId,
    Guid StatusId,
    string Objective,
    DateOnly ScheduledDate,
    DateTime? StartedAtUtc,
    string ExecutionMode,
    DateTime CreatedAtUtc);

public sealed record AuditListPageResult(IReadOnlyList<AuditListRow> Items, int Total);

/// <summary>Puerto de acceso a <see cref="Audit"/>. Deliberadamente NO es un repositorio genérico.
/// </summary>
public interface IAuditRepository
{
    /// <summary><c>GET /api/audits/{id}</c> y los endpoints de escritura: carga la auditoría CON
    /// <see cref="Audit.Programs"/> y <see cref="Audit.Team"/> ya incluidos. <c>null</c> si no
    /// existe dentro del tenant.</summary>
    Task<Audit?> GetByIdAsync(Guid tenantId, Guid auditId, CancellationToken cancellationToken);

    Task AddAsync(Audit audit, CancellationToken cancellationToken);

    /// <summary>Unicidad de <c>(tenant_id, folio)</c> — usada al generar el folio en <c>CreateAudit</c>
    /// para descartar, en el caso extremo, una colisión del componente aleatorio.</summary>
    Task<bool> ExistsFolioAsync(Guid tenantId, string folio, CancellationToken cancellationToken);

    /// <summary><c>GET /api/audits</c>: listado paginado del tenant actual. <paramref
    /// name="clientScope"/> <c>null</c> = sin restricción (ADMIN/auditores); un conjunto no-nulo
    /// (posiblemente vacío) acota la lectura a esos <c>clientId</c> — alcance de CLIENTE vía <see
    /// cref="Procofa.Application.UseCases.Clients.ClientAccessScope"/>, nunca interpretado como
    /// "sin restricción". <paramref name="search"/> busca en folio/objective con ILIKE.</summary>
    Task<AuditListPageResult> ListAsync(
        Guid tenantId,
        Guid? clientId,
        Guid? companyId,
        string? status,
        Guid? auditTypeId,
        string? executionMode,
        string? search,
        int page,
        int pageSize,
        IReadOnlyCollection<Guid>? clientScope,
        CancellationToken cancellationToken);
}
