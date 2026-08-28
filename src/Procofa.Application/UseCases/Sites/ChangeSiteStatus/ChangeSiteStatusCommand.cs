namespace Procofa.Application.UseCases.Sites.ChangeSiteStatus;

public sealed record ChangeSiteStatusCommand(Guid ClientId, Guid CompanyId, Guid SiteId, bool IsActive);
