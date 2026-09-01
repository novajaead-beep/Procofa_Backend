namespace Procofa.Application.UseCases.Checklists.ChangeChecklistStatus;

public enum ChangeChecklistStatusError
{
    NotFound,
}

public sealed class ChangeChecklistStatusResult
{
    public bool IsSuccess { get; }
    public ChangeChecklistStatusError? Error { get; }

    private ChangeChecklistStatusResult(bool isSuccess, ChangeChecklistStatusError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ChangeChecklistStatusResult Success() => new(true, null);

    public static ChangeChecklistStatusResult Failure(ChangeChecklistStatusError error) => new(false, error);
}
