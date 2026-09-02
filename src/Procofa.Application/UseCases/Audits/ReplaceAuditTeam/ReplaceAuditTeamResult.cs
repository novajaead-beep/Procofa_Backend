namespace Procofa.Application.UseCases.Audits.ReplaceAuditTeam;

public enum ReplaceAuditTeamError
{
    ValidationFailed,
    NotFound,
    NotEditable,
    UserNotFound,
    InvalidRole,
    DuplicateUser,
    MultipleLeads,
}

public sealed class ReplaceAuditTeamResult
{
    public bool IsSuccess { get; }
    public ReplaceAuditTeamError? Error { get; }
    public string? ErrorDetail { get; }

    private ReplaceAuditTeamResult(bool isSuccess, ReplaceAuditTeamError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static ReplaceAuditTeamResult Success() => new(true, null, null);

    public static ReplaceAuditTeamResult Failure(ReplaceAuditTeamError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
