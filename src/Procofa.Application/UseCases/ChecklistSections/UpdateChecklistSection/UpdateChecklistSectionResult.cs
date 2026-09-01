namespace Procofa.Application.UseCases.ChecklistSections.UpdateChecklistSection;

public enum UpdateChecklistSectionError
{
    ValidationFailed,
    NotFound,
    VersionPublished,
}

public sealed class UpdateChecklistSectionResult
{
    public bool IsSuccess { get; }
    public UpdateChecklistSectionError? Error { get; }

    private UpdateChecklistSectionResult(bool isSuccess, UpdateChecklistSectionError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static UpdateChecklistSectionResult Success() => new(true, null);

    public static UpdateChecklistSectionResult Failure(UpdateChecklistSectionError error) => new(false, error);
}
