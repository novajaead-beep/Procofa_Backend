namespace Procofa.Domain.Entities.Clients;

/// <summary>
/// Sitio/planta físico de una <see cref="AuditedCompany"/>. Entidad
/// independiente con <c>DbSet</c> propio (ver justificación en
/// <see cref="AuditedCompany"/>) — referenciada externamente por
/// <c>audits.company_site_id</c>.
/// Tabla física: <c>company_sites</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>audited_companies</c>.
/// </summary>
public sealed class CompanySite
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditedCompanyId { get; private set; }
    public string Name { get; private set; } = null!;
    public string AddressLine1 { get; private set; } = null!;
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? StateRegion { get; private set; }
    public string? PostalCode { get; private set; }
    public string Country { get; private set; } = "México";

    /// <summary><c>numeric(9,6)</c> — ver <c>CompanySiteConfiguration</c>.</summary>
    public decimal? Latitude { get; private set; }

    /// <summary><c>numeric(9,6)</c> — ver <c>CompanySiteConfiguration</c>.</summary>
    public decimal? Longitude { get; private set; }

    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_company_sites_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private CompanySite() { }

    public CompanySite(
        Guid id,
        Guid tenantId,
        Guid auditedCompanyId,
        string name,
        string addressLine1,
        string? addressLine2,
        string? city,
        string? stateRegion,
        string? postalCode,
        string country,
        decimal? latitude,
        decimal? longitude)
    {
        Id = id;
        TenantId = tenantId;
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
        IsActive = true;
    }

    /// <summary><c>PUT /api/clients/{clientId}/companies/{companyId}/sites/{siteId}</c>: reemplaza
    /// los campos editables. Nunca toca <see cref="Id"/>/<see cref="TenantId"/>/<see
    /// cref="AuditedCompanyId"/>/<see cref="CreatedAtUtc"/>.</summary>
    public void UpdateDetails(
        string name, string addressLine1, string? addressLine2, string? city, string? stateRegion,
        string? postalCode, string country, decimal? latitude, decimal? longitude)
    {
        Name = name;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        StateRegion = stateRegion;
        PostalCode = postalCode;
        Country = country;
        Latitude = latitude;
        Longitude = longitude;
    }

    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>Soft — nunca hard delete.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
