namespace Procofa.Application.UseCases.ChecklistVersions.GetChecklistVersion;

public enum GetChecklistVersionError
{
    NotFound,
}

public sealed class GetChecklistVersionResult
{
    public bool IsSuccess { get; }
    public GetChecklistVersionError? Error { get; }
    public Guid Id { get; }
    public int VersionNumber { get; }
    public string Status { get; } = string.Empty;
    public string? ChangeNotes { get; }
    public DateTime? PublishedAtUtc { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; }

    private GetChecklistVersionResult(bool isSuccess, GetChecklistVersionError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    private GetChecklistVersionResult(
        Guid id, int versionNumber, string status, string? changeNotes, DateTime? publishedAtUtc,
        DateTime createdAtUtc, DateTime updatedAtUtc)
        : this(true, null)
    {
        Id = id;
        VersionNumber = versionNumber;
        Status = status;
        ChangeNotes = changeNotes;
        PublishedAtUtc = publishedAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GetChecklistVersionResult Success(
        Guid id, int versionNumber, string status, string? changeNotes, DateTime? publishedAtUtc,
        DateTime createdAtUtc, DateTime updatedAtUtc) =>
        new(id, versionNumber, status, changeNotes, publishedAtUtc, createdAtUtc, updatedAtUtc);

    public static GetChecklistVersionResult NotFound() => new(false, GetChecklistVersionError.NotFound);
}
