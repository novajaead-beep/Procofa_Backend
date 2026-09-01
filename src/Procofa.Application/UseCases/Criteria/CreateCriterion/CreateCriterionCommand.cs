using Procofa.Domain.Enums;

namespace Procofa.Application.UseCases.Criteria.CreateCriterion;

public sealed record CreateCriterionCommand(
    Guid ChecklistId,
    Guid VersionId,
    Guid SectionId,
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
