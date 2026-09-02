namespace Procofa.Application.UseCases.Audits.GetAudit;

public enum GetAuditError
{
    NotFound,
}

public sealed record GetAuditTeamMemberItem(Guid UserId, string Role, Guid? AssignedByUserId, DateTime AssignedAtUtc);

public sealed record GetAuditChecklistItem(
    Guid AuditChecklistId, Guid ChecklistId, Guid ChecklistVersionId, int VersionNumber, string ChecklistName);

public sealed class GetAuditResult
{
    public bool IsSuccess { get; }
    public GetAuditError? Error { get; }
    public Guid Id { get; }
    public string Folio { get; } = string.Empty;
    public Guid ClientId { get; }
    public Guid AuditedCompanyId { get; }
    public Guid? CompanySiteId { get; }
    public Guid AuditTypeId { get; }
    public Guid ProfileId { get; }
    public Guid StatusId { get; }
    public string Objective { get; } = string.Empty;
    public string Scope { get; } = string.Empty;
    public string? Methodology { get; }
    public DateOnly ScheduledDate { get; }
    public DateTime? StartedAtUtc { get; }
    public DateTime? FinishedAtUtc { get; }
    public DateTime? ClosedAtUtc { get; }
    public string ExecutionMode { get; } = string.Empty;
    public bool IsEditable { get; }
    public IReadOnlyCollection<string> ProgramCodes { get; } = [];
    public IReadOnlyCollection<GetAuditTeamMemberItem> Team { get; } = [];
    public IReadOnlyCollection<GetAuditChecklistItem> Checklists { get; } = [];
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; }

    private GetAuditResult(bool isSuccess, GetAuditError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    private GetAuditResult(
        Guid id, string folio, Guid clientId, Guid auditedCompanyId, Guid? companySiteId, Guid auditTypeId,
        Guid profileId, Guid statusId, string objective, string scope, string? methodology,
        DateOnly scheduledDate, DateTime? startedAtUtc, DateTime? finishedAtUtc, DateTime? closedAtUtc,
        string executionMode, bool isEditable, IReadOnlyCollection<string> programCodes,
        IReadOnlyCollection<GetAuditTeamMemberItem> team, IReadOnlyCollection<GetAuditChecklistItem> checklists,
        DateTime createdAtUtc, DateTime updatedAtUtc)
        : this(true, null)
    {
        Id = id;
        Folio = folio;
        ClientId = clientId;
        AuditedCompanyId = auditedCompanyId;
        CompanySiteId = companySiteId;
        AuditTypeId = auditTypeId;
        ProfileId = profileId;
        StatusId = statusId;
        Objective = objective;
        Scope = scope;
        Methodology = methodology;
        ScheduledDate = scheduledDate;
        StartedAtUtc = startedAtUtc;
        FinishedAtUtc = finishedAtUtc;
        ClosedAtUtc = closedAtUtc;
        ExecutionMode = executionMode;
        IsEditable = isEditable;
        ProgramCodes = programCodes;
        Team = team;
        Checklists = checklists;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GetAuditResult Success(
        Guid id, string folio, Guid clientId, Guid auditedCompanyId, Guid? companySiteId, Guid auditTypeId,
        Guid profileId, Guid statusId, string objective, string scope, string? methodology,
        DateOnly scheduledDate, DateTime? startedAtUtc, DateTime? finishedAtUtc, DateTime? closedAtUtc,
        string executionMode, bool isEditable, IReadOnlyCollection<string> programCodes,
        IReadOnlyCollection<GetAuditTeamMemberItem> team, IReadOnlyCollection<GetAuditChecklistItem> checklists,
        DateTime createdAtUtc, DateTime updatedAtUtc) =>
        new(id, folio, clientId, auditedCompanyId, companySiteId, auditTypeId, profileId, statusId, objective,
            scope, methodology, scheduledDate, startedAtUtc, finishedAtUtc, closedAtUtc, executionMode, isEditable,
            programCodes, team, checklists, createdAtUtc, updatedAtUtc);

    public static GetAuditResult NotFound() => new(false, GetAuditError.NotFound);
}
