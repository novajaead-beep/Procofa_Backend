namespace Procofa.Application.UseCases.Audits.UpdateAudit;

/// <summary><c>PUT /api/audits/{auditId}</c>. <c>ClientId</c> es inmutable post-creación —
/// deliberadamente fuera de este command.</summary>
public sealed record UpdateAuditCommand(
    Guid AuditId,
    Guid? AuditedCompanyId,
    Guid? CompanySiteId,
    Guid? AuditTypeId,
    Guid? ProfileId,
    string? Objective,
    string? Scope,
    string? Methodology,
    DateOnly? ScheduledDate,
    string? ExecutionMode);
