namespace Procofa.Application.UseCases.Sites.ListSites;

/// <summary><c>GET /api/clients/{clientId}/companies/{companyId}/sites</c>.</summary>
public sealed record ListSitesQuery(Guid ClientId, Guid CompanyId);
