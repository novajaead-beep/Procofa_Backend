namespace Procofa.Api.Contracts.Companies;

public sealed record CompanyListItemResponse(
    Guid Id, string LegalName, string? TradeName, string? TaxId, bool IsClientCompany, bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CompanyListResponse(
    IReadOnlyCollection<CompanyListItemResponse> Items, int Page, int PageSize, int Total);
