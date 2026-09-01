namespace Procofa.Application.UseCases.ChecklistVersions.PublishChecklistVersion;

public enum PublishChecklistVersionError
{
    NotFound,
    AlreadyPublished,
    NoSections,
    NoCriteria,
}

public sealed class PublishChecklistVersionResult
{
    public bool IsSuccess { get; }
    public PublishChecklistVersionError? Error { get; }

    private PublishChecklistVersionResult(bool isSuccess, PublishChecklistVersionError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static PublishChecklistVersionResult Success() => new(true, null);

    public static PublishChecklistVersionResult Failure(PublishChecklistVersionError error) => new(false, error);
}
