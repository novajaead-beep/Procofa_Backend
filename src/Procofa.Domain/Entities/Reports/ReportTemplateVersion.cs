using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Reports;

/// <summary>
/// Contenido versionado de un <see cref="ReportTemplate"/>.
/// Tabla física: <c>report_template_versions</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE RESTRICT</c> desde <c>report_templates</c>. Referenciada
/// externamente por <c>audit_reports.report_template_version_id</c>.
/// Invariante <c>(report_template_id, version_number)</c> único.
///
/// <see cref="ConfigurationJson"/> mapea la columna <c>jsonb</c> como texto
/// crudo (<c>string</c>) — Domain se mantiene agnóstico de
/// System.Text.Json/Newtonsoft; Infrastructure configura
/// <c>.HasColumnType("jsonb")</c> explícitamente.
/// </summary>
public sealed class ReportTemplateVersion
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ReportTemplateId { get; private set; }
    public int VersionNumber { get; private set; }
    public ReportTemplateVersionStatus Status { get; private set; } = ReportTemplateVersionStatus.Draft;
    public string TemplateStorageKey { get; private set; } = null!;
    public string? ConfigurationJson { get; private set; }
    public string? ChangeNotes { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_report_template_versions_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private ReportTemplateVersion() { }

    public ReportTemplateVersion(
        Guid id,
        Guid tenantId,
        Guid reportTemplateId,
        int versionNumber,
        string templateStorageKey,
        string? configurationJson,
        Guid createdByUserId)
    {
        Id = id;
        TenantId = tenantId;
        ReportTemplateId = reportTemplateId;
        VersionNumber = versionNumber;
        TemplateStorageKey = templateStorageKey;
        ConfigurationJson = configurationJson;
        CreatedByUserId = createdByUserId;
        Status = ReportTemplateVersionStatus.Draft;
    }
}
