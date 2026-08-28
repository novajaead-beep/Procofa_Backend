using Procofa.Domain.Entities.Clients.ValueObjects;

namespace Procofa.Domain.Entities.Clients;

/// <summary>
/// Cliente que contrata el servicio de auditoría. Aggregate Root.
/// Tabla física: <c>clients</c>, tenant-scoped, RLS+FORCE RLS.
///
/// Posee <see cref="Programs"/> (tabla <c>client_programs</c>, PK compuesta
/// <c>(client_id, program_id)</c>, sin columna <c>id</c> → colección owned,
/// sin <c>DbSet</c> propio).
///
/// <c>ClientContact</c> y <c>AuditedCompany</c> están conceptualmente
/// dentro del límite transaccional de este aggregate (alta/edición de
/// cliente + sus contactos + programas asociados — baseline V2.1, sección F)
/// pero se modelan como entidades independientes con <c>DbSet</c> propio,
/// no como tipos owned de EF: ambas son referenciadas por FK desde OTROS
/// aggregates (<c>audit_signatories.client_contact_id</c>,
/// <c>findings.responsible_contact_id</c>,
/// <c>corrective_actions.responsible_contact_id</c>,
/// <c>audits.audited_company_id</c>) y EF "owned types" no está pensado
/// para ser referenciado por FK desde fuera de su dueño. La consistencia
/// del aggregate se enforza en Application (guard de validación
/// intra-auditoría / intra-cliente), no vía la forma de mapeo de EF.
///
/// Invariante <c>tax_id</c> único por tenant (si se provee) — ver índice
/// parcial <c>uq_clients_tenant_tax_id</c> en <c>ClientConfiguration</c>.
/// </summary>
public sealed class Client
{
    private readonly List<ClientProgram> _programs = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string LegalName { get; private set; } = null!;
    public string? TradeName { get; private set; }
    public string? TaxId { get; private set; }
    public string? Industry { get; private set; }
    public string? CompanyType { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_clients_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<ClientProgram> Programs => _programs.AsReadOnly();

    private Client() { }

    public Client(
        Guid id,
        Guid tenantId,
        string legalName,
        string? tradeName,
        string? taxId,
        string? industry,
        string? companyType,
        string? notes)
    {
        Id = id;
        TenantId = tenantId;
        LegalName = legalName;
        TradeName = tradeName;
        TaxId = taxId;
        Industry = industry;
        CompanyType = companyType;
        Notes = notes;
        IsActive = true;
    }

    /// <summary><c>PUT /api/clients/{id}</c>: reemplaza los campos editables. Nunca toca <see
    /// cref="Id"/>/<see cref="TenantId"/>/<see cref="CreatedAtUtc"/>.</summary>
    public void UpdateDetails(
        string legalName, string? tradeName, string? taxId, string? industry, string? companyType, string? notes)
    {
        LegalName = legalName;
        TradeName = tradeName;
        TaxId = taxId;
        Industry = industry;
        CompanyType = companyType;
        Notes = notes;
    }

    /// <summary><c>PATCH /api/clients/{id}/status</c>: reactiva el cliente (nunca borra ni resetea
    /// nada más).</summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary><c>PATCH /api/clients/{id}/status</c>: desactiva el cliente (soft — nunca hard
    /// delete).</summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary><c>PUT /api/clients/{id}</c> (cuando el request trae programas): reemplaza el
    /// conjunto completo de <see cref="Programs"/> — nunca hace merge parcial. Mismo mecanismo de
    /// detección de cambios que <c>User.ReplaceRoles</c>.</summary>
    public void ReplacePrograms(IEnumerable<ClientProgram> newPrograms)
    {
        _programs.Clear();
        _programs.AddRange(newPrograms);
    }
}
