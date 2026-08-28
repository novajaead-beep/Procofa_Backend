namespace Procofa.Api.Contracts.Sites;

public sealed record UpdateSiteRequest(
    string? Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateRegion,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);
