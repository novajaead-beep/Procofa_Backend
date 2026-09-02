namespace Procofa.Api.Contracts.Audits;

public sealed record AuditListItemResponse(
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

public sealed record AuditListResponse(
    IReadOnlyCollection<AuditListItemResponse> Items, int Page, int PageSize, int Total);
