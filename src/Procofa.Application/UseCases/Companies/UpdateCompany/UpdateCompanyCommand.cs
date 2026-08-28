namespace Procofa.Application.UseCases.Companies.UpdateCompany;

public sealed record UpdateCompanyCommand(
    Guid ClientId,
    Guid CompanyId,
    Guid? DefaultProfileId,
    string? LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    bool IsClientCompany);
