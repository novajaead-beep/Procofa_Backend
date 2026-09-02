namespace Procofa.Application.UseCases.Audits.CreateAudit;

public enum CreateAuditError
{
    ValidationFailed,
    ClientNotFound,
    AuditedCompanyNotFound,
    CompanySiteNotFound,
    AuditTypeNotFound,
    ProfileNotFound,
    ProgramNotFound,
    StatusNotFound,
}

public sealed class CreateAuditResult
{
    public bool IsSuccess { get; }
    public CreateAuditError? Error { get; }
    public string? ErrorDetail { get; }
    public Guid? AuditId { get; }
    public string? Folio { get; }

    private CreateAuditResult(
        bool isSuccess, CreateAuditError? error, string? errorDetail, Guid? auditId, string? folio)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
        AuditId = auditId;
        Folio = folio;
    }

    public static CreateAuditResult Success(Guid auditId, string folio) => new(true, null, null, auditId, folio);

    public static CreateAuditResult Failure(CreateAuditError error, string? errorDetail = null) =>
        new(false, error, errorDetail, null, null);
}
