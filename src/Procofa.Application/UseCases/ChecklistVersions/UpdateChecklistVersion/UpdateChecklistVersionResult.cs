namespace Procofa.Application.UseCases.ChecklistVersions.UpdateChecklistVersion;

public enum UpdateChecklistVersionError
{
    NotFound,
    VersionPublished,
}

public sealed class UpdateChecklistVersionResult
{
    public bool IsSuccess { get; }
    public UpdateChecklistVersionError? Error { get; }

    private UpdateChecklistVersionResult(bool isSuccess, UpdateChecklistVersionError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static UpdateChecklistVersionResult Success() => new(true, null);

    public static UpdateChecklistVersionResult Failure(UpdateChecklistVersionError error) => new(false, error);
}
