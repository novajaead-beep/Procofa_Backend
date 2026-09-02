namespace Procofa.Api.Contracts.Audits;

public sealed record AuditTeamMemberResponse(Guid UserId, string Role, Guid? AssignedByUserId, DateTime AssignedAtUtc);

public sealed record AuditChecklistItemResponse(
    Guid AuditChecklistId, Guid ChecklistId, Guid ChecklistVersionId, int VersionNumber, string ChecklistName);

public sealed record AuditDetailResponse(
    Guid Id,
    string Folio,
    Guid ClientId,
    Guid AuditedCompanyId,
    Guid? CompanySiteId,
    Guid AuditTypeId,
    Guid ProfileId,
    Guid StatusId,
    string Objective,
    string Scope,
    string? Methodology,
    DateOnly ScheduledDate,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    DateTime? ClosedAtUtc,
    string ExecutionMode,
    bool IsEditable,
    IReadOnlyCollection<string> ProgramCodes,
    IReadOnlyCollection<AuditTeamMemberResponse> Team,
    IReadOnlyCollection<AuditChecklistItemResponse> Checklists,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
