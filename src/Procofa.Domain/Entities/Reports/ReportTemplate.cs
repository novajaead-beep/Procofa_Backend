using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Reports;

/// <summary>
/// Encabezado/familia de plantilla de reporte. Sigue el mismo patrón dual
/// que <c>Checklist</c>/<c>ChecklistVersion</c>: el contenido versionado
/// vive en <see cref="ReportTemplateVersion"/> (RESTRICT en cascada,
/// versionado, protección del histórico — baseline V2.1 sección H).
/// Tabla física: <c>report_templates</c>, tenant-scoped, RLS+FORCE RLS.
/// Invariante <c>(tenant_id, code)</c> único.
/// </summary>
public sealed class ReportTemplate
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public ReportType ReportType { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_report_templates_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private ReportTemplate() { }

    public ReportTemplate(
        Guid id,
        Guid tenantId,
        string code,
        string name,
        ReportType reportType,
        string? description,
        Guid createdByUserId)
    {
        Id = id;
        TenantId = tenantId;
        Code = code;
        Name = name;
        ReportType = reportType;
        Description = description;
        CreatedByUserId = createdByUserId;
        IsActive = true;
    }
}
