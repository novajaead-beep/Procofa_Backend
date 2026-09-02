namespace Procofa.Api.Contracts.Audits;

public sealed record CreateAuditRequest(
    Guid? ClientId,
    Guid? AuditedCompanyId,
    Guid? CompanySiteId,
    Guid? AuditTypeId,
    Guid? ProfileId,
    IReadOnlyCollection<string>? ProgramCodes,
    string? Objective,
    string? Scope,
    string? Methodology,
    DateOnly? ScheduledDate,
    string? ExecutionMode);

public sealed record CreateAuditResponse(Guid Id, string Folio);
