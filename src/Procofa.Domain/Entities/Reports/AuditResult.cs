namespace Procofa.Domain.Entities.Reports;

/// <summary>
/// Resultado consolidado de una auditoría — 1:1 con <c>Audit</c>. Entidad
/// independiente con <c>DbSet</c> propio (no EF owned type, por uniformidad
/// del criterio de mapeo). Se finaliza junto con el cierre de la auditoría.
/// Tabla física: <c>audit_results</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>audits</c>.
/// Invariante 1:1 — ver <c>audit_results_audit_id_key UNIQUE(audit_id)</c>
/// en <c>AuditResultConfiguration</c>.
/// </summary>
public sealed class AuditResult
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public string? ExecutiveSummary { get; private set; }
    public string? GeneralResult { get; private set; }
    public string? Conclusions { get; private set; }
    public string? GeneralRecommendations { get; private set; }

    /// <summary><c>numeric(5,2)</c>, CHECK NULL o entre 0 y 100.</summary>
    public decimal? CompliancePercentage { get; private set; }

    public int EvaluatedCriteriaCount { get; private set; }
    public int CompliantCriteriaCount { get; private set; }
    public int PartiallyCompliantCriteriaCount { get; private set; }
    public int NonCompliantCriteriaCount { get; private set; }
    public int NotApplicableCriteriaCount { get; private set; }
    public Guid? FinalizedByUserId { get; private set; }
    public DateTime? FinalizedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_audit_results_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private AuditResult() { }

    public AuditResult(Guid id, Guid tenantId, Guid auditId)
    {
        Id = id;
        TenantId = tenantId;
        AuditId = auditId;
        EvaluatedCriteriaCount = 0;
        CompliantCriteriaCount = 0;
        PartiallyCompliantCriteriaCount = 0;
        NonCompliantCriteriaCount = 0;
        NotApplicableCriteriaCount = 0;
    }
}
