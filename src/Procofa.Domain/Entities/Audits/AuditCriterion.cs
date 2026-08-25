namespace Procofa.Domain.Entities.Audits;

/// <summary>
/// Evaluación concreta de un <c>Criterion</c> dentro de una
/// <see cref="Audit"/> — snapshot inmutable del criterio + respuesta del
/// auditor. Aggregate Root propio, separado de <see cref="Audit"/>
/// (evidencia: <see cref="LockVersion"/> propio existe precisamente para
/// que dos evaluadores editando criterios distintos de la misma auditoría
/// no contiendan entre sí — anidarlo dentro de <c>Audit</c> obligaría a
/// bloquear toda la auditoría en cada autosave; baseline V2.1 sección F).
///
/// Posee <c>Observation</c> (historial de comentarios, <c>ON DELETE CASCADE</c>
/// desde <c>audit_criterion_id</c>) — modelada como entidad independiente
/// con <c>DbSet</c> propio por uniformidad del criterio de mapeo (toda tabla
/// con columna <c>id</c> propia), no como EF owned type.
///
/// Tabla física: <c>audit_criteria</c>, tenant-scoped, RLS+FORCE RLS.
/// <see cref="LockVersion"/>: concurrencia optimista —
/// <c>.IsConcurrencyToken()</c>, incremento responsabilidad de Application/EF
/// (sin trigger que lo incremente).
/// Invariante <c>(audit_id, criterion_id)</c> único — ver
/// <c>uq_audit_criterion</c> en <c>AuditCriterionConfiguration</c>.
/// Índice parcial <c>ix_audit_criteria_pending WHERE compliance_status_id IS NULL</c>.
/// </summary>
public sealed class AuditCriterion
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid AuditChecklistId { get; private set; }
    public Guid CriterionId { get; private set; }
    public Guid? ComplianceStatusId { get; private set; }

    /// <summary>Snapshot inmutable tomado al crear el registro.</summary>
    public string CriterionCodeSnapshot { get; private set; } = null!;

    /// <summary>Snapshot inmutable tomado al crear el registro.</summary>
    public string QuestionSnapshot { get; private set; } = null!;

    /// <summary>Snapshot inmutable tomado al crear el registro.</summary>
    public string? NormativeReferenceSnapshot { get; private set; }

    /// <summary>Snapshot inmutable tomado al crear el registro.</summary>
    public bool IsMandatorySnapshot { get; private set; }

    public string? AuditedResponse { get; private set; }
    public string? IdentifiedRisk { get; private set; }
    public string? Recommendation { get; private set; }
    public Guid? EvaluatedByUserId { get; private set; }
    public DateTime? EvaluatedAtUtc { get; private set; }

    /// <summary>
    /// <c>bigint DEFAULT 1 NOT NULL CHECK (lock_version &gt; 0)</c>.
    /// Token de concurrencia optimista — <c>.IsConcurrencyToken()</c> en
    /// <c>AuditCriterionConfiguration</c>. Sin trigger que lo incremente: el
    /// incremento es responsabilidad de un <c>SaveChangesInterceptor</c> de
    /// Infrastructure (ver <c>ConcurrencyTokenInterceptor</c>).
    /// </summary>
    public long LockVersion { get; private set; } = 1;

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_audit_criteria_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private AuditCriterion() { }

    public AuditCriterion(
        Guid id,
        Guid tenantId,
        Guid auditId,
        Guid auditChecklistId,
        Guid criterionId,
        string criterionCodeSnapshot,
        string questionSnapshot,
        string? normativeReferenceSnapshot,
        bool isMandatorySnapshot)
    {
        Id = id;
        TenantId = tenantId;
        AuditId = auditId;
        AuditChecklistId = auditChecklistId;
        CriterionId = criterionId;
        CriterionCodeSnapshot = criterionCodeSnapshot;
        QuestionSnapshot = questionSnapshot;
        NormativeReferenceSnapshot = normativeReferenceSnapshot;
        IsMandatorySnapshot = isMandatorySnapshot;
        LockVersion = 1;
    }
}
