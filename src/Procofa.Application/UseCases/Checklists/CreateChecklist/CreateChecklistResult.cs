namespace Procofa.Application.UseCases.Checklists.CreateChecklist;

public enum CreateChecklistError
{
    ValidationFailed,
    ProgramNotFound,
    ProfileNotFound,
    AuditTypeNotFound,
}

public sealed class CreateChecklistResult
{
    public bool IsSuccess { get; }
    public CreateChecklistError? Error { get; }
    public string? ErrorDetail { get; }
    public Guid? ChecklistId { get; }

    private CreateChecklistResult(bool isSuccess, CreateChecklistError? error, string? errorDetail, Guid? checklistId)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
        ChecklistId = checklistId;
    }

    public static CreateChecklistResult Success(Guid checklistId) => new(true, null, null, checklistId);

    public static CreateChecklistResult Failure(CreateChecklistError error, string? errorDetail = null) =>
        new(false, error, errorDetail, null);
}
