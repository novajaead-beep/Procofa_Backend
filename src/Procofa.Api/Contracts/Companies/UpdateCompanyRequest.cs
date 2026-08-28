namespace Procofa.Api.Contracts.Companies;

public sealed record UpdateCompanyRequest(
    Guid? DefaultProfileId,
    string? LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    bool IsClientCompany);
