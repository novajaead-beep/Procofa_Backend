namespace Procofa.Domain.Entities.Audits;

/// <summary>
/// Fija la <c>ChecklistVersion</c> congelada que usa una <see cref="Audit"/>
/// concreta. Entidad independiente con <c>DbSet</c> propio — referenciada
/// externamente por <c>audit_criteria.audit_checklist_id</c>.
/// Tabla física: <c>audit_checklists</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>audits</c>,
/// <c>ON DELETE RESTRICT</c> desde <c>checklist_versions</c> (protege el
/// histórico de la versión usada).
/// Invariante <c>(audit_id, checklist_version_id)</c> único — ver
/// <c>uq_audit_checklist_version</c> en <c>AuditChecklistConfiguration</c>.
/// </summary>
public sealed class AuditChecklist
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid ChecklistVersionId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private AuditChecklist() { }

    public AuditChecklist(Guid id, Guid tenantId, Guid auditId, Guid checklistVersionId)
    {
        Id = id;
        TenantId = tenantId;
        AuditId = auditId;
        ChecklistVersionId = checklistVersionId;
    }
}
