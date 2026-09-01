namespace Procofa.Application.UseCases.Criteria.CreateCriterion;

public enum CreateCriterionError
{
    ValidationFailed,
    SectionNotFound,
    VersionPublished,
    CodeAlreadyExists,
}

public sealed class CreateCriterionResult
{
    public bool IsSuccess { get; }
    public CreateCriterionError? Error { get; }
    public Guid? CriterionId { get; }

    private CreateCriterionResult(bool isSuccess, CreateCriterionError? error, Guid? criterionId)
    {
        IsSuccess = isSuccess;
        Error = error;
        CriterionId = criterionId;
    }

    public static CreateCriterionResult Success(Guid criterionId) => new(true, null, criterionId);

    public static CreateCriterionResult Failure(CreateCriterionError error) => new(false, error, null);
}
