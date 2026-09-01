namespace Procofa.Application.UseCases.ChecklistSections.ListChecklistSections;

public sealed record ChecklistSectionListItem(
    Guid Id, string? Code, string Name, string? Description, int SortOrder);

public enum ListChecklistSectionsError
{
    VersionNotFound,
}

public sealed class ListChecklistSectionsResult
{
    public bool IsSuccess { get; }
    public ListChecklistSectionsError? Error { get; }
    public IReadOnlyList<ChecklistSectionListItem> Items { get; } = [];

    private ListChecklistSectionsResult(bool isSuccess, ListChecklistSectionsError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    private ListChecklistSectionsResult(IReadOnlyList<ChecklistSectionListItem> items)
        : this(true, null)
    {
        Items = items;
    }

    public static ListChecklistSectionsResult Success(IReadOnlyList<ChecklistSectionListItem> items) => new(items);

    public static ListChecklistSectionsResult Failure(ListChecklistSectionsError error) => new(false, error);
}
