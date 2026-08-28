namespace Procofa.Application.UseCases.Sites.CreateSite;

/// <summary><c>POST /api/clients/{clientId}/companies/{companyId}/sites</c>. <see
/// cref="ClientId"/>/<see cref="CompanyId"/> vienen siempre de la ruta.</summary>
public sealed record CreateSiteCommand(
    Guid ClientId,
    Guid CompanyId,
    string? Name,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateRegion,
    string? PostalCode,
    string? Country,
    decimal? Latitude,
    decimal? Longitude);
