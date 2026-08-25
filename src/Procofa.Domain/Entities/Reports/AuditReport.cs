using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Reports;

/// <summary>
/// Documento de reporte generado (versión, formato, estado). Aggregate Root
/// (baseline V2.1 sección F).
/// Tabla física: <c>audit_reports</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>audits</c>. A diferencia de la mayoría
/// de las tablas de este dominio, NO tiene columnas
/// <c>created_at_utc</c>/<c>updated_at_utc</c> — <see cref="GeneratedAtUtc"/>
/// cumple el rol de timestamp de creación, y no existe trigger de
/// "updated_at" (usa <c>trg_audit_reports_final_immutable</c> en su lugar,
/// que bloquea UPDATE/DELETE cuando <see cref="Status"/> = FINAL).
///
/// Un reporte <see cref="AuditReportStatus.Final"/> es inmutable — trigger
/// <c>prevent_final_report_mutation()</c>, versión correcta (<c>DELETE</c>
/// retorna <c>OLD</c>, no <c>NEW</c> — baseline V2.1 sección D).
/// Invariante <c>(audit_id, report_type, version_number, format)</c> único.
/// </summary>
public sealed class AuditReport
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid? ReportTemplateVersionId { get; private set; }
    public ReportType ReportType { get; private set; }
    public int VersionNumber { get; private set; } = 1;
    public ReportFormat Format { get; private set; }
    public AuditReportStatus Status { get; private set; } = AuditReportStatus.Draft;
    public string StorageKey { get; private set; } = null!;
    public string? Sha256Hex { get; private set; }
    public Guid GeneratedByUserId { get; private set; }
    public Guid? ValidatedByUserId { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
    public DateTime? ValidatedAtUtc { get; private set; }

    private AuditReport() { }

    public AuditReport(
        Guid id,
        Guid tenantId,
        Guid auditId,
        Guid? reportTemplateVersionId,
        ReportType reportType,
        ReportFormat format,
        string storageKey,
        Guid generatedByUserId)
    {
        Id = id;
        TenantId = tenantId;
        AuditId = auditId;
        ReportTemplateVersionId = reportTemplateVersionId;
        ReportType = reportType;
        VersionNumber = 1;
        Format = format;
        Status = AuditReportStatus.Draft;
        StorageKey = storageKey;
        GeneratedByUserId = generatedByUserId;
    }
}
