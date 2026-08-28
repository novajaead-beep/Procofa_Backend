namespace Procofa.Application.UseCases.Sites.ListSites;

public enum ListSitesError
{
    /// <summary>El client no es visible, o la company no existe/no pertenece al client.</summary>
    CompanyNotFound,
}

public sealed record SiteListItem(
    Guid Id, string Name, string? City, bool IsActive);

public sealed class ListSitesResult
{
    public bool IsSuccess { get; }
    public ListSitesError? Error { get; }
    public IReadOnlyList<SiteListItem> Items { get; } = [];

    private ListSitesResult(bool isSuccess, ListSitesError? error) { IsSuccess = isSuccess; Error = error; }

    private ListSitesResult(IReadOnlyList<SiteListItem> items) : this(true, null) => Items = items;

    public static ListSitesResult Success(IReadOnlyList<SiteListItem> items) => new(items);

    public static ListSitesResult Failure(ListSitesError error) => new(false, error);
}
