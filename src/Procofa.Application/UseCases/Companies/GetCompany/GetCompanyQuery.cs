namespace Procofa.Application.UseCases.Companies.GetCompany;

/// <summary><c>GET /api/clients/{clientId}/companies/{companyId}</c>.</summary>
public sealed record GetCompanyQuery(Guid ClientId, Guid CompanyId);
