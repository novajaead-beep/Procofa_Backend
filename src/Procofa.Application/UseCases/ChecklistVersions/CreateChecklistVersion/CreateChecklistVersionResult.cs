namespace Procofa.Application.UseCases.ChecklistVersions.CreateChecklistVersion;

public enum CreateChecklistVersionError
{
    ChecklistNotFound,
}

public sealed class CreateChecklistVersionResult
{
    public bool IsSuccess { get; }
    public CreateChecklistVersionError? Error { get; }
    public Guid? VersionId { get; }
    public int? VersionNumber { get; }

    private CreateChecklistVersionResult(
        bool isSuccess, CreateChecklistVersionError? error, Guid? versionId, int? versionNumber)
    {
        IsSuccess = isSuccess;
        Error = error;
        VersionId = versionId;
        VersionNumber = versionNumber;
    }

    public static CreateChecklistVersionResult Success(Guid versionId, int versionNumber) =>
        new(true, null, versionId, versionNumber);

    public static CreateChecklistVersionResult Failure(CreateChecklistVersionError error) =>
        new(false, error, null, null);
}
