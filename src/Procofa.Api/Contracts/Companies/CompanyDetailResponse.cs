namespace Procofa.Api.Contracts.Companies;

public sealed record CompanyDetailResponse(
    Guid Id,
    Guid ClientId,
    Guid? DefaultProfileId,
    string LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    bool IsClientCompany,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
