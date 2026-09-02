namespace Procofa.Application.UseCases.Audits.UpdateAudit;

public enum UpdateAuditError
{
    ValidationFailed,
    NotFound,
    NotEditable,
    AuditedCompanyNotFound,
    CompanySiteNotFound,
    AuditTypeNotFound,
    ProfileNotFound,
    ChecklistIncompatible,
}

public sealed class UpdateAuditResult
{
    public bool IsSuccess { get; }
    public UpdateAuditError? Error { get; }
    public string? ErrorDetail { get; }

    private UpdateAuditResult(bool isSuccess, UpdateAuditError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static UpdateAuditResult Success() => new(true, null, null);

    public static UpdateAuditResult Failure(UpdateAuditError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
