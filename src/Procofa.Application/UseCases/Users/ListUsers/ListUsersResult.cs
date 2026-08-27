using Procofa.Application.Abstractions.Identity;

namespace Procofa.Application.UseCases.Users.ListUsers;

public enum ListUsersError
{
    /// <summary><c>role</c> no pertenece al catálogo cerrado de <see cref="UserRoleCodes"/>.</summary>
    InvalidRole,
}

/// <summary>Resultado de <see cref="ListUsersQueryHandler"/> — construido únicamente vía <see cref="Success"/>/<see cref="Failure"/>.</summary>
public sealed class ListUsersResult
{
    public bool IsSuccess { get; }
    public ListUsersError? Error { get; }
    public IReadOnlyList<UserListRow> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int Total { get; }

    private ListUsersResult(
        bool isSuccess, ListUsersError? error, IReadOnlyList<UserListRow> items, int page, int pageSize, int total)
    {
        IsSuccess = isSuccess;
        Error = error;
        Items = items;
        Page = page;
        PageSize = pageSize;
        Total = total;
    }

    public static ListUsersResult Success(IReadOnlyList<UserListRow> items, int page, int pageSize, int total) =>
        new(true, null, items, page, pageSize, total);

    public static ListUsersResult Failure(ListUsersError error) =>
        new(false, error, [], 0, 0, 0);
}
