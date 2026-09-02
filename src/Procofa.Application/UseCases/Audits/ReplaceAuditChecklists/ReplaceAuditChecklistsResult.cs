namespace Procofa.Application.UseCases.Audits.ReplaceAuditChecklists;

public enum ReplaceAuditChecklistsError
{
    NotFound,
    NotEditable,
    ChecklistNotFound,
    IncompatibleChecklist,
    NoPublishedVersion,
}

public sealed class ReplaceAuditChecklistsResult
{
    public bool IsSuccess { get; }
    public ReplaceAuditChecklistsError? Error { get; }
    public string? ErrorDetail { get; }

    private ReplaceAuditChecklistsResult(bool isSuccess, ReplaceAuditChecklistsError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static ReplaceAuditChecklistsResult Success() => new(true, null, null);

    public static ReplaceAuditChecklistsResult Failure(
        ReplaceAuditChecklistsError error, string? errorDetail = null) => new(false, error, errorDetail);
}
