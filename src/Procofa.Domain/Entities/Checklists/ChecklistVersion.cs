using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Checklists;

/// <summary>
/// Contenido evaluable de una versión concreta de <see cref="Checklist"/>.
/// Aggregate Root propio (baseline V2.1 sección F). <see cref="ChecklistSection"/>
/// y <c>Criterion</c> están conceptualmente poseídas por esta versión
/// (editables solo mientras <see cref="Status"/> = <see cref="ChecklistVersionStatus.Draft"/>)
/// pero se modelan como entidades independientes con <c>DbSet</c> propio
/// porque <c>Criterion</c> es referenciada externamente por
/// <c>audit_criteria.criterion_id</c> — mismo razonamiento que
/// <c>ClientContact</c>/<c>CompanySite</c> en el grupo Clientes.
///
/// Tabla física: <c>checklist_versions</c>, tenant-scoped, RLS+FORCE RLS.
/// Invariante <c>version_number</c> único por checklist — ver
/// <c>uq_checklist_version</c> en <c>ChecklistVersionConfiguration</c>.
///
/// La inmutabilidad de una versión <see cref="ChecklistVersionStatus.Published"/>
/// se enforza en Application por ahora (baseline V2.1, hallazgo 🟢 sección C);
/// no hay trigger SQL equivalente a <c>prevent_final_report_mutation()</c>
/// todavía.
/// </summary>
public sealed class ChecklistVersion
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ChecklistId { get; private set; }
    public int VersionNumber { get; private set; }
    public ChecklistVersionStatus Status { get; private set; } = ChecklistVersionStatus.Draft;
    public string? ChangeNotes { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_checklist_versions_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private ChecklistVersion() { }

    public ChecklistVersion(Guid id, Guid tenantId, Guid checklistId, int versionNumber, Guid createdByUserId)
    {
        Id = id;
        TenantId = tenantId;
        ChecklistId = checklistId;
        VersionNumber = versionNumber;
        CreatedByUserId = createdByUserId;
        Status = ChecklistVersionStatus.Draft;
    }
}
