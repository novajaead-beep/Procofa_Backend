using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Checklists;

/// <summary>
/// Criterio evaluable (plantilla) dentro de una <see cref="ChecklistSection"/>.
/// Físicamente distinta de <c>AuditCriterion</c> (snapshot evaluado dentro de
/// una auditoría concreta) — baseline V2.1 sección D. Entidad independiente
/// con <c>DbSet</c> propio, referenciada externamente por
/// <c>audit_criteria.criterion_id</c>.
/// Tabla física: <c>criteria</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE RESTRICT</c> desde <c>checklist_sections</c>.
/// Sin <c>created_at_utc</c>/<c>updated_at_utc</c> — fidelidad física.
///
/// Invariante <c>(checklist_section_id, code)</c> único — ver
/// <c>uq_criteria_section_code</c> en <c>CriterionConfiguration</c>.
/// </summary>
public sealed class Criterion
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ChecklistSectionId { get; private set; }
    public string Code { get; private set; } = null!;
    public string AuditQuestion { get; private set; } = null!;
    public string? AuditorInterpretation { get; private set; }
    public string? ExpectedEvidence { get; private set; }

    /// <summary>
    /// <c>criteria.evidence_type varchar(80)</c> — texto libre descriptivo
    /// ("qué evidencia se espera"), SIN CHECK constraint en la BD real
    /// (a diferencia de <c>audit_evidences.evidence_type</c>, que sí es un
    /// enum <see cref="Enums.EvidenceType"/>). Nombrada
    /// <see cref="ExpectedEvidenceType"/> — no <c>EvidenceType</c> — para no
    /// confundirla con ese enum ni colisionar el nombre en este archivo.
    /// </summary>
    public string? ExpectedEvidenceType { get; private set; }

    public ImportanceLevel? ImportanceLevel { get; private set; }
    public string? NormativeReference { get; private set; }
    public string? EvaluationRecommendation { get; private set; }
    public bool IsMandatory { get; private set; } = true;
    public int SortOrder { get; private set; }

    private Criterion() { }

    public Criterion(
        Guid id,
        Guid tenantId,
        Guid checklistSectionId,
        string code,
        string auditQuestion,
        string? auditorInterpretation,
        string? expectedEvidence,
        string? expectedEvidenceType,
        ImportanceLevel? importanceLevel,
        string? normativeReference,
        string? evaluationRecommendation,
        bool isMandatory,
        int sortOrder)
    {
        Id = id;
        TenantId = tenantId;
        ChecklistSectionId = checklistSectionId;
        Code = code;
        AuditQuestion = auditQuestion;
        AuditorInterpretation = auditorInterpretation;
        ExpectedEvidence = expectedEvidence;
        ExpectedEvidenceType = expectedEvidenceType;
        ImportanceLevel = importanceLevel;
        NormativeReference = normativeReference;
        EvaluationRecommendation = evaluationRecommendation;
        IsMandatory = isMandatory;
        SortOrder = sortOrder;
    }
}
