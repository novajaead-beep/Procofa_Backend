namespace Procofa.Application.UseCases.Checklists.ResolveChecklist;

public enum ResolveChecklistError
{
    ValidationFailed,
    NotFound,
}

public sealed class ResolveChecklistResult
{
    public bool IsSuccess { get; }
    public ResolveChecklistError? Error { get; }
    public string? ErrorDetail { get; }
    public Guid ChecklistId { get; }
    public string ChecklistName { get; } = string.Empty;
    public Guid VersionId { get; }
    public int VersionNumber { get; }
    public bool IsExactMatch { get; }

    private ResolveChecklistResult(bool isSuccess, ResolveChecklistError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    private ResolveChecklistResult(
        Guid checklistId, string checklistName, Guid versionId, int versionNumber, bool isExactMatch)
        : this(true, null, null)
    {
        ChecklistId = checklistId;
        ChecklistName = checklistName;
        VersionId = versionId;
        VersionNumber = versionNumber;
        IsExactMatch = isExactMatch;
    }

    public static ResolveChecklistResult Success(
        Guid checklistId, string checklistName, Guid versionId, int versionNumber, bool isExactMatch) =>
        new(checklistId, checklistName, versionId, versionNumber, isExactMatch);

    public static ResolveChecklistResult Failure(ResolveChecklistError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
