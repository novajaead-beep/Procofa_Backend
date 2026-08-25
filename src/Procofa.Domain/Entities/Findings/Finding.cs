namespace Procofa.Domain.Entities.Findings;

/// <summary>
/// No conformidad/observación/oportunidad de mejora detectada durante una
/// auditoría, ligada a un <c>AuditCriterion</c> concreto (obligatorio).
/// Aggregate Root (baseline V2.1 sección F).
///
/// Posee <see cref="FindingFollowup"/> (bitácora de seguimiento) —
/// modelada como entidad independiente con <c>DbSet</c> propio por
/// uniformidad del criterio de mapeo, no como EF owned type.
///
/// Tabla física: <c>findings</c>, tenant-scoped, RLS+FORCE RLS.
/// <see cref="LockVersion"/>: concurrencia optimista propia — un
/// AUDITOR_LIDER validando este Finding no debe contender con evaluaciones
/// de otros criterios de la misma auditoría.
///
/// Invariante <c>(audit_id, finding_number)</c> único — ver
/// <c>uq_findings_audit_number</c> en <c>FindingConfiguration</c>. Riesgo
/// operativo documentado (baseline V2.1 sección K): <c>finding_number</c> NO
/// tiene secuencia/default en BD — nunca calcular con <c>MAX+1</c> sin
/// sincronización (responsabilidad de la futura <c>CreateFindingUseCase</c>,
/// no de esta instrucción).
/// Índice parcial <c>ix_findings_commitment_date WHERE closed_at_utc IS NULL</c>.
///
/// Validación de consistencia intra-auditoría (patrón general): el
/// <c>AuditCriterion</c> referenciado debe pertenecer a
/// <see cref="AuditId"/> — se enforza en Application, no aquí.
/// </summary>
public sealed class Finding
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid AuditCriterionId { get; private set; }

    /// <summary>CHECK &gt; 0. Único junto con <see cref="AuditId"/>.</summary>
    public int FindingNumber { get; private set; }

    public Guid FindingTypeId { get; private set; }
    public Guid PriorityId { get; private set; }
    public Guid StatusId { get; private set; }
    public string? Title { get; private set; }
    public string Description { get; private set; } = null!;
    public string? ObservedEvidence { get; private set; }
    public string? RiskImpact { get; private set; }
    public string? ViolatedRequirement { get; private set; }
    public string? Recommendation { get; private set; }
    public Guid? ResponsibleUserId { get; private set; }
    public Guid? ResponsibleContactId { get; private set; }
    public DateOnly? CommitmentDate { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? ValidatedByUserId { get; private set; }
    public DateTime? ValidatedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    /// <summary>
    /// <c>bigint DEFAULT 1 NOT NULL CHECK (lock_version &gt; 0)</c>.
    /// Ver <c>ConcurrencyTokenInterceptor</c>.
    /// </summary>
    public long LockVersion { get; private set; } = 1;

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_findings_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private Finding() { }

    public Finding(
        Guid id,
        Guid tenantId,
        Guid auditId,
        Guid auditCriterionId,
        int findingNumber,
        Guid findingTypeId,
        Guid priorityId,
        Guid statusId,
        string? title,
        string description,
        Guid createdByUserId)
    {
        Id = id;
        TenantId = tenantId;
        AuditId = auditId;
        AuditCriterionId = auditCriterionId;
        FindingNumber = findingNumber;
        FindingTypeId = findingTypeId;
        PriorityId = priorityId;
        StatusId = statusId;
        Title = title;
        Description = description;
        CreatedByUserId = createdByUserId;
        LockVersion = 1;
    }
}
