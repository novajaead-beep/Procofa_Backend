namespace Procofa.Application.UseCases.Companies.ListCompanies;

public enum ListCompaniesError
{
    /// <summary>El client no existe, pertenece a otro tenant, o no es visible bajo el alcance de
    /// CLIENTE.</summary>
    ClientNotFound,
}

public sealed record CompanyListItem(
    Guid Id,
    string LegalName,
    string? TradeName,
    string? TaxId,
    bool IsClientCompany,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed class ListCompaniesResult
{
    public bool IsSuccess { get; }
    public ListCompaniesError? Error { get; }
    public IReadOnlyList<CompanyListItem> Items { get; } = [];
    public int Page { get; }
    public int PageSize { get; }
    public int Total { get; }

    private ListCompaniesResult(bool isSuccess, ListCompaniesError? error) { IsSuccess = isSuccess; Error = error; }

    private ListCompaniesResult(IReadOnlyList<CompanyListItem> items, int page, int pageSize, int total)
        : this(true, null)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        Total = total;
    }

    public static ListCompaniesResult Success(IReadOnlyList<CompanyListItem> items, int page, int pageSize, int total) =>
        new(items, page, pageSize, total);

    public static ListCompaniesResult Failure(ListCompaniesError error) => new(false, error);
}
