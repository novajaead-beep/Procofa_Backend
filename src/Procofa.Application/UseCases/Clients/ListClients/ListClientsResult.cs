namespace Procofa.Application.UseCases.Clients.ListClients;

public sealed record ClientListItem(
    Guid Id,
    string LegalName,
    string? TradeName,
    string? TaxId,
    bool IsActive,
    IReadOnlyCollection<string> Programs,
    int AuditedCompanyCount,
    DateTime CreatedAtUtc);

public sealed class ListClientsResult
{
    public IReadOnlyList<ClientListItem> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int Total { get; }

    public ListClientsResult(IReadOnlyList<ClientListItem> items, int page, int pageSize, int total)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        Total = total;
    }
}
