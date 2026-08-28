namespace Procofa.Application.UseCases.Companies.ListCompanies;

/// <summary><c>GET /api/clients/{clientId}/companies</c>.</summary>
public sealed record ListCompaniesQuery(Guid ClientId, string? Search, bool? IsActive, int Page, int PageSize);
