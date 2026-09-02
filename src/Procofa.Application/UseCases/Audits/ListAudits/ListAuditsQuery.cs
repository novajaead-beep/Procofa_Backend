namespace Procofa.Application.UseCases.Audits.ListAudits;

/// <summary><c>GET /api/audits</c>. <see cref="Status"/> es el código físico de
/// <c>audit_statuses.code</c> (ej. "BORRADOR"); <see cref="ExecutionMode"/> el string físico de
/// <c>audits.execution_mode</c>.</summary>
public sealed record ListAuditsQuery(
    Guid? ClientId,
    Guid? CompanyId,
    string? Status,
    Guid? AuditTypeId,
    string? ExecutionMode,
    string? Search,
    int Page,
    int PageSize);
