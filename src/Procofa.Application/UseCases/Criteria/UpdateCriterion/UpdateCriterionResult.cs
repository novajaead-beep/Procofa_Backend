namespace Procofa.Application.UseCases.Criteria.UpdateCriterion;

public enum UpdateCriterionError
{
    ValidationFailed,
    NotFound,
    VersionPublished,
    CodeAlreadyExists,
}

public sealed class UpdateCriterionResult
{
    public bool IsSuccess { get; }
    public UpdateCriterionError? Error { get; }

    private UpdateCriterionResult(bool isSuccess, UpdateCriterionError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static UpdateCriterionResult Success() => new(true, null);

    public static UpdateCriterionResult Failure(UpdateCriterionError error) => new(false, error);
}
