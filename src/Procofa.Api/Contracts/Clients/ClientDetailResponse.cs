namespace Procofa.Api.Contracts.Clients;

public sealed record ClientDetailResponse(
    Guid Id,
    string LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    string? Notes,
    bool IsActive,
    IReadOnlyCollection<string> Programs,
    int AuditedCompanyCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
