namespace Procofa.Domain.Entities.Clients;

/// <summary>
/// Organización física que se audita — puede o no coincidir con el propio
/// <c>Client</c>. Aggregate Root propio, NO contenido dentro de
/// <c>Client</c> (evidencia física: <c>audited_companies.client_id</c> es
/// <c>ON DELETE RESTRICT</c>, no CASCADE — protege a AuditedCompany de
/// desaparecer si se toca Client; señal de aggregates independientes con
/// referencia por ID, baseline V2.1 sección F).
/// Tabla física: <c>audited_companies</c>, tenant-scoped, RLS+FORCE RLS.
///
/// <c>CompanySite</c> está conceptualmente poseída por este aggregate
/// (<c>ON DELETE CASCADE</c> real) pero se modela como entidad independiente
/// con <c>DbSet</c> propio porque <c>audits.company_site_id</c> la referencia
/// desde otro aggregate — mismo razonamiento que <c>ClientContact</c> en
/// <see cref="Client"/>.
///
/// Invariante <c>tax_id</c> único por (tenant, client) si se provee — ver
/// índice parcial <c>uq_audited_company_client_tax_id</c> en
/// <c>AuditedCompanyConfiguration</c>.
/// </summary>
public sealed class AuditedCompany
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid? DefaultProfileId { get; private set; }
    public string LegalName { get; private set; } = null!;
    public string? TradeName { get; private set; }
    public string? TaxId { get; private set; }
    public string? Industry { get; private set; }
    public string? CompanyType { get; private set; }
    public bool IsClientCompany { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_audited_companies_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private AuditedCompany() { }

    public AuditedCompany(
        Guid id,
        Guid tenantId,
        Guid clientId,
        Guid? defaultProfileId,
        string legalName,
        string? tradeName,
        string? taxId,
        string? industry,
        string? companyType,
        bool isClientCompany)
    {
        Id = id;
        TenantId = tenantId;
        ClientId = clientId;
        DefaultProfileId = defaultProfileId;
        LegalName = legalName;
        TradeName = tradeName;
        TaxId = taxId;
        Industry = industry;
        CompanyType = companyType;
        IsClientCompany = isClientCompany;
        IsActive = true;
    }
}
