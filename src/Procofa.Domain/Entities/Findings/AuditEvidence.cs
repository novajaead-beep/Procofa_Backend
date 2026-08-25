using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Findings;

/// <summary>
/// Evidencia cargada dentro de una auditoría — puede asociarse
/// opcionalmente a un <c>AuditCriterion</c>, <see cref="Finding"/>,
/// <see cref="CorrectiveAction"/> y/o <c>AuditDocumentRequest</c> (los
/// cuatro son nullable e independientes entre sí a nivel de columna).
/// Entidad independiente con <c>DbSet</c> propio.
/// Tabla física: <c>audit_evidences</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>audits</c>,
/// <c>ON DELETE SET NULL</c> desde criterion/finding/corrective_action/document_request.
///
/// Validación de consistencia intra-auditoría (patrón general, baseline
/// V2.1 sección F): si trae <c>AuditCriterionId</c>/<c>FindingId</c>/
/// <c>CorrectiveActionId</c>, cada uno debe resolver transitivamente al
/// mismo <c>AuditId</c> — se enforza en Application (guard reusable), NO en
/// esta instrucción de persistencia.
/// SHA-256 y validaciones de tamaño/MIME real se resuelven síncronamente
/// durante la carga (decisión congelada #7) — no en esta instrucción.
/// </summary>
public sealed class AuditEvidence
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid? AuditCriterionId { get; private set; }
    public Guid? FindingId { get; private set; }
    public Guid? CorrectiveActionId { get; private set; }
    public Guid? DocumentRequestId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public EvidenceType EvidenceType { get; private set; }
    public string OriginalFileName { get; private set; } = null!;

    /// <summary>Clave/ruta en el almacenamiento (S3-compatible, futuro).</summary>
    public string StorageKey { get; private set; } = null!;

    public string? MimeType { get; private set; }

    /// <summary>CHECK: NULL o &gt;= 0.</summary>
    public long? FileSizeBytes { get; private set; }

    public string? Sha256Hex { get; private set; }
    public string? Description { get; private set; }
    public bool IsReportRelevant { get; private set; } = true;
    public bool IncludeInReport { get; private set; } = true;
    public bool IncludeAsAnnex { get; private set; }

    /// <summary>CHECK: NULL o &gt; 0.</summary>
    public int? AnnexOrder { get; private set; }

    public string? Caption { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AuditEvidence() { }

    public AuditEvidence(
        Guid id,
        Guid tenantId,
        Guid auditId,
        Guid? auditCriterionId,
        Guid? findingId,
        Guid? correctiveActionId,
        Guid? documentRequestId,
        Guid uploadedByUserId,
        EvidenceType evidenceType,
        string originalFileName,
        string storageKey,
        string? mimeType,
        long? fileSizeBytes,
        string? sha256Hex)
    {
        Id = id;
        TenantId = tenantId;
        AuditId = auditId;
        AuditCriterionId = auditCriterionId;
        FindingId = findingId;
        CorrectiveActionId = correctiveActionId;
        DocumentRequestId = documentRequestId;
        UploadedByUserId = uploadedByUserId;
        EvidenceType = evidenceType;
        OriginalFileName = originalFileName;
        StorageKey = storageKey;
        MimeType = mimeType;
        FileSizeBytes = fileSizeBytes;
        Sha256Hex = sha256Hex;
        IsReportRelevant = true;
        IncludeInReport = true;
        IncludeAsAnnex = false;
    }
}
