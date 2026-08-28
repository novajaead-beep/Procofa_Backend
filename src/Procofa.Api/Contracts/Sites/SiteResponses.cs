namespace Procofa.Api.Contracts.Sites;

public sealed record SiteListItemResponse(Guid Id, string Name, string? City, bool IsActive);

public sealed record SiteListResponse(IReadOnlyCollection<SiteListItemResponse> Items);

public sealed record SiteDetailResponse(
    Guid Id,
    Guid AuditedCompanyId,
    string Name,
    string AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateRegion,
    string? PostalCode,
    string Country,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
