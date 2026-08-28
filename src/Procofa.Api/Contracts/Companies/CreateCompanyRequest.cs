namespace Procofa.Api.Contracts.Companies;

public sealed record CreateCompanyRequest(
    Guid? DefaultProfileId,
    string? LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    bool IsClientCompany);

public sealed record CreateCompanyResponse(Guid Id);
