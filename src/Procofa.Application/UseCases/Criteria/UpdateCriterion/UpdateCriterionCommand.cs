using Procofa.Domain.Enums;

namespace Procofa.Application.UseCases.Criteria.UpdateCriterion;

public sealed record UpdateCriterionCommand(
    Guid ChecklistId,
    Guid VersionId,
    Guid SectionId,
    Guid CriterionId,
    string? Code,
    string? AuditQuestion,
    string? AuditorInterpretation,
    string? ExpectedEvidence,
    string? ExpectedEvidenceType,
    ImportanceLevel? ImportanceLevel,
    string? NormativeReference,
    string? EvaluationRecommendation,
    bool IsMandatory,
    int SortOrder);
