namespace Procofa.Application.UseCases.Checklists.UpdateChecklist;

public enum UpdateChecklistError
{
    NotFound,
    ValidationFailed,
    ProgramNotFound,
    ProfileNotFound,
    AuditTypeNotFound,
}

public sealed class UpdateChecklistResult
{
    public bool IsSuccess { get; }
    public UpdateChecklistError? Error { get; }
    public string? ErrorDetail { get; }

    private UpdateChecklistResult(bool isSuccess, UpdateChecklistError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static UpdateChecklistResult Success() => new(true, null, null);

    public static UpdateChecklistResult Failure(UpdateChecklistError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
