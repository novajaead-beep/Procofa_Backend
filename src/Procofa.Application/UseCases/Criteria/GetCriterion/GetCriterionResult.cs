using Procofa.Domain.Enums;

namespace Procofa.Application.UseCases.Criteria.GetCriterion;

public enum GetCriterionError
{
    NotFound,
}

public sealed class GetCriterionResult
{
    public bool IsSuccess { get; }
    public GetCriterionError? Error { get; }
    public Guid Id { get; }
    public string Code { get; } = string.Empty;
    public string AuditQuestion { get; } = string.Empty;
    public string? AuditorInterpretation { get; }
    public string? ExpectedEvidence { get; }
    public string? ExpectedEvidenceType { get; }
    public ImportanceLevel? ImportanceLevel { get; }
    public string? NormativeReference { get; }
    public string? EvaluationRecommendation { get; }
    public bool IsMandatory { get; }
    public int SortOrder { get; }

    private GetCriterionResult(bool isSuccess, GetCriterionError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    private GetCriterionResult(
        Guid id, string code, string auditQuestion, string? auditorInterpretation, string? expectedEvidence,
        string? expectedEvidenceType, ImportanceLevel? importanceLevel, string? normativeReference,
        string? evaluationRecommendation, bool isMandatory, int sortOrder)
        : this(true, null)
    {
        Id = id;
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

    public static GetCriterionResult Success(
        Guid id, string code, string auditQuestion, string? auditorInterpretation, string? expectedEvidence,
        string? expectedEvidenceType, ImportanceLevel? importanceLevel, string? normativeReference,
        string? evaluationRecommendation, bool isMandatory, int sortOrder) =>
        new(id, code, auditQuestion, auditorInterpretation, expectedEvidence, expectedEvidenceType, importanceLevel,
            normativeReference, evaluationRecommendation, isMandatory, sortOrder);

    public static GetCriterionResult NotFound() => new(false, GetCriterionError.NotFound);
}
