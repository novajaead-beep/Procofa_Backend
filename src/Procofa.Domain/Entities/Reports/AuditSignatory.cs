using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Reports;

/// <summary>
/// Firmante de una auditoría. Tiene <see cref="AuditId"/> directo — NO
/// <c>report_id</c>: pertenece a la <c>Audit</c>, no al
/// <see cref="AuditReport"/> (baseline V2.1 sección F, nota explícita).
/// Entidad independiente con <c>DbSet</c> propio.
/// Tabla física: <c>audit_signatories</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>audits</c>,
/// <c>ON DELETE SET NULL</c> desde <c>users</c>/<c>client_contacts</c>.
///
/// <c>ck_audit_signatory_source</c> (al menos uno de <see cref="UserId"/>/
/// <see cref="ClientContactId"/>/<see cref="SignerName"/> no nulo) se
/// replica como <c>.HasCheckConstraint(...)</c> por fidelidad, aunque
/// <see cref="SignerName"/> ya es <c>NOT NULL</c> a nivel de columna física
/// (el CHECK es, en la práctica, siempre satisfecho — se mapea tal cual
/// existe, sin "corregir" la BD real).
/// </summary>
public sealed class AuditSignatory
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? ClientContactId { get; private set; }
    public string SignerName { get; private set; } = null!;
    public string? SignerRole { get; private set; }
    public SignerType SignerType { get; private set; }
    public string? SignatureStorageKey { get; private set; }
    public DateTime? SignedAtUtc { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_audit_signatories_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private AuditSignatory() { }

    public AuditSignatory(
        Guid id,
        Guid tenantId,
        Guid auditId,
        Guid? userId,
        Guid? clientContactId,
        string signerName,
        string? signerRole,
        SignerType signerType,
        int sortOrder)
    {
        Id = id;
        TenantId = tenantId;
        AuditId = auditId;
        UserId = userId;
        ClientContactId = clientContactId;
        SignerName = signerName;
        SignerRole = signerRole;
        SignerType = signerType;
        SortOrder = sortOrder;
    }
}
