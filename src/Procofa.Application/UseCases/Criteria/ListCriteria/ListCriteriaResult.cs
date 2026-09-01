namespace Procofa.Application.UseCases.Criteria.ListCriteria;

public sealed record CriterionListItem(
    Guid Id, string Code, string AuditQuestion, bool IsMandatory, int SortOrder);

public enum ListCriteriaError
{
    SectionNotFound,
}

public sealed class ListCriteriaResult
{
    public bool IsSuccess { get; }
    public ListCriteriaError? Error { get; }
    public IReadOnlyList<CriterionListItem> Items { get; } = [];

    private ListCriteriaResult(bool isSuccess, ListCriteriaError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    private ListCriteriaResult(IReadOnlyList<CriterionListItem> items)
        : this(true, null)
    {
        Items = items;
    }

    public static ListCriteriaResult Success(IReadOnlyList<CriterionListItem> items) => new(items);

    public static ListCriteriaResult Failure(ListCriteriaError error) => new(false, error);
}
