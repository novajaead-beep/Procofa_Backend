namespace Procofa.Application.UseCases.Sites.GetSite;

public sealed record GetSiteQuery(Guid ClientId, Guid CompanyId, Guid SiteId);
