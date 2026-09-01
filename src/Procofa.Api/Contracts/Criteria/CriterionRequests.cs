namespace Procofa.Api.Contracts.Criteria;

public sealed record CreateCriterionRequest(
    string? Code,
    string? AuditQuestion,
    string? AuditorInterpretation,
    string? ExpectedEvidence,
    string? ExpectedEvidenceType,
    string? ImportanceLevel,
    string? NormativeReference,
    string? EvaluationRecommendation,
    bool IsMandatory,
    int SortOrder);

public sealed record UpdateCriterionRequest(
    string? Code,
    string? AuditQuestion,
    string? AuditorInterpretation,
    string? ExpectedEvidence,
    string? ExpectedEvidenceType,
    string? ImportanceLevel,
    string? NormativeReference,
    string? EvaluationRecommendation,
    bool IsMandatory,
    int SortOrder);

public sealed record CreateCriterionResponse(Guid Id);
