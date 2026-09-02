namespace Procofa.Application.UseCases.Audits.ReplaceAuditPrograms;

public enum ReplaceAuditProgramsError
{
    NotFound,
    NotEditable,
    ProgramNotFound,
    ChecklistOrphaned,
}

public sealed class ReplaceAuditProgramsResult
{
    public bool IsSuccess { get; }
    public ReplaceAuditProgramsError? Error { get; }
    public string? ErrorDetail { get; }

    private ReplaceAuditProgramsResult(bool isSuccess, ReplaceAuditProgramsError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static ReplaceAuditProgramsResult Success() => new(true, null, null);

    public static ReplaceAuditProgramsResult Failure(ReplaceAuditProgramsError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
