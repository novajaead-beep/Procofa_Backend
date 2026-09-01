namespace Procofa.Application.UseCases.Criteria.DeleteCriterion;

public enum DeleteCriterionError
{
    NotFound,
    VersionPublished,
}

public sealed class DeleteCriterionResult
{
    public bool IsSuccess { get; }
    public DeleteCriterionError? Error { get; }

    private DeleteCriterionResult(bool isSuccess, DeleteCriterionError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static DeleteCriterionResult Success() => new(true, null);

    public static DeleteCriterionResult Failure(DeleteCriterionError error) => new(false, error);
}
