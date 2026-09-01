namespace Procofa.Application.UseCases.ChecklistVersions.ListChecklistVersions;

public sealed record ChecklistVersionListItem(
    Guid Id, int VersionNumber, string Status, DateTime? PublishedAtUtc, DateTime CreatedAtUtc);

public enum ListChecklistVersionsError
{
    ChecklistNotFound,
}

public sealed class ListChecklistVersionsResult
{
    public bool IsSuccess { get; }
    public ListChecklistVersionsError? Error { get; }
    public IReadOnlyList<ChecklistVersionListItem> Items { get; } = [];

    private ListChecklistVersionsResult(bool isSuccess, ListChecklistVersionsError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    private ListChecklistVersionsResult(IReadOnlyList<ChecklistVersionListItem> items)
        : this(true, null)
    {
        Items = items;
    }

    public static ListChecklistVersionsResult Success(IReadOnlyList<ChecklistVersionListItem> items) => new(items);

    public static ListChecklistVersionsResult Failure(ListChecklistVersionsError error) => new(false, error);
}
