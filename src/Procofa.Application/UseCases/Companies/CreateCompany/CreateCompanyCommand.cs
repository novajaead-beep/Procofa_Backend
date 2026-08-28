namespace Procofa.Application.UseCases.Companies.CreateCompany;

/// <summary><c>POST /api/clients/{clientId}/companies</c>. <see cref="ClientId"/> viene siempre de
/// la ruta — nunca del body.</summary>
public sealed record CreateCompanyCommand(
    Guid ClientId,
    Guid? DefaultProfileId,
    string? LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    bool IsClientCompany);
