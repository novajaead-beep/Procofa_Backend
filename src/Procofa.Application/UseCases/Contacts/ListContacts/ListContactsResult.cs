namespace Procofa.Application.UseCases.Contacts.ListContacts;

public enum ListContactsError
{
    ClientNotFound,
}

public sealed record ContactListItem(
    Guid Id, string FirstName, string LastName, string? JobTitle, string? Email, string? Phone, bool IsActive);

public sealed class ListContactsResult
{
    public bool IsSuccess { get; }
    public ListContactsError? Error { get; }
    public IReadOnlyList<ContactListItem> Items { get; } = [];

    private ListContactsResult(bool isSuccess, ListContactsError? error) { IsSuccess = isSuccess; Error = error; }

    private ListContactsResult(IReadOnlyList<ContactListItem> items) : this(true, null) => Items = items;

    public static ListContactsResult Success(IReadOnlyList<ContactListItem> items) => new(items);

    public static ListContactsResult Failure(ListContactsError error) => new(false, error);
}
