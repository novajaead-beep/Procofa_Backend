namespace Procofa.Api.Contracts.Criteria;

public sealed record CriterionListItemResponse(Guid Id, string Code, string AuditQuestion, bool IsMandatory, int SortOrder);

public sealed record CriterionListResponse(IReadOnlyCollection<CriterionListItemResponse> Items);

public sealed record CriterionDetailResponse(
    Guid Id,
    string Code,
    string AuditQuestion,
    string? AuditorInterpretation,
    string? ExpectedEvidence,
    string? ExpectedEvidenceType,
    string? ImportanceLevel,
    string? NormativeReference,
    string? EvaluationRecommendation,
    bool IsMandatory,
    int SortOrder);
