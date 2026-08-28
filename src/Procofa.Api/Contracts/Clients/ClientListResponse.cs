namespace Procofa.Api.Contracts.Clients;

public sealed record ClientListItemResponse(
    Guid Id,
    string LegalName,
    string? TradeName,
    string? TaxId,
    bool IsActive,
    IReadOnlyCollection<string> Programs,
    int AuditedCompanyCount,
    DateTime CreatedAtUtc);

public sealed record ClientListResponse(
    IReadOnlyCollection<ClientListItemResponse> Items, int Page, int PageSize, int Total);
