namespace Procofa.Application.UseCases.Sites.GetSite;

public enum GetSiteError
{
    NotFound,
}

public sealed class GetSiteResult
{
    public bool IsSuccess { get; }
    public GetSiteError? Error { get; }
    public Guid Id { get; }
    public Guid AuditedCompanyId { get; }
    public string Name { get; } = string.Empty;
    public string AddressLine1 { get; } = string.Empty;
    public string? AddressLine2 { get; }
    public string? City { get; }
    public string? StateRegion { get; }
    public string? PostalCode { get; }
    public string Country { get; } = string.Empty;
    public decimal? Latitude { get; }
    public decimal? Longitude { get; }
    public bool IsActive { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; }

    private GetSiteResult(bool isSuccess, GetSiteError? error) { IsSuccess = isSuccess; Error = error; }

    private GetSiteResult(
        Guid id, Guid auditedCompanyId, string name, string addressLine1, string? addressLine2, string? city,
        string? stateRegion, string? postalCode, string country, decimal? latitude, decimal? longitude,
        bool isActive, DateTime createdAtUtc, DateTime updatedAtUtc)
        : this(true, null)
    {
        Id = id;
        AuditedCompanyId = auditedCompanyId;
        Name = name;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        StateRegion = stateRegion;
        PostalCode = postalCode;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GetSiteResult Success(
        Guid id, Guid auditedCompanyId, string name, string addressLine1, string? addressLine2, string? city,
        string? stateRegion, string? postalCode, string country, decimal? latitude, decimal? longitude,
        bool isActive, DateTime createdAtUtc, DateTime updatedAtUtc) =>
        new(id, auditedCompanyId, name, addressLine1, addressLine2, city, stateRegion, postalCode, country,
            latitude, longitude, isActive, createdAtUtc, updatedAtUtc);

    public static GetSiteResult NotFound() => new(false, GetSiteError.NotFound);
}
