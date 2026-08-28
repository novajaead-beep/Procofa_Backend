namespace Procofa.Application.UseCases.Sites.UpdateSite;

public sealed record UpdateSiteCommand(
    Guid ClientId,
    Guid CompanyId,
    Guid SiteId,
    string? Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateRegion,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);
