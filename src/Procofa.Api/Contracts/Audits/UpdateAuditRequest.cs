namespace Procofa.Api.Contracts.Audits;

public sealed record UpdateAuditRequest(
    Guid? AuditedCompanyId,
    Guid? CompanySiteId,
    Guid? AuditTypeId,
    Guid? ProfileId,
    string? Objective,
    string? Scope,
    string? Methodology,
    DateOnly? ScheduledDate,
    string? ExecutionMode);
