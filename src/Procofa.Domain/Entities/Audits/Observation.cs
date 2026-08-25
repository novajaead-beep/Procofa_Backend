using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Audits;

/// <summary>
/// Comentario/observación sobre un <see cref="AuditCriterion"/> — historial,
/// no snapshot editable. Entidad independiente con <c>DbSet</c> propio
/// (ver justificación de uniformidad en <see cref="AuditCriterion"/>).
/// Tabla física: <c>observations</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>audits</c> y desde <c>audit_criteria</c>.
/// Sin <c>lock_version</c> — es historial append-style, no un recurso editado
/// con concurrencia optimista.
/// </summary>
public sealed class Observation
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid AuditCriterionId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public ObservationType ObservationType { get; private set; } = ObservationType.Auditor;
    public string Description { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    private Observation() { }

    public Observation(
        Guid id,
        Guid tenantId,
        Guid auditId,
        Guid auditCriterionId,
        Guid authorUserId,
        ObservationType observationType,
        string description)
    {
        Id = id;
        TenantId = tenantId;
        AuditId = auditId;
        AuditCriterionId = auditCriterionId;
        AuthorUserId = authorUserId;
        ObservationType = observationType;
        Description = description;
    }
}
