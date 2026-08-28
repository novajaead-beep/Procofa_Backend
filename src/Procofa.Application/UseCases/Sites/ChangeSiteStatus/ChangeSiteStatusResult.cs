namespace Procofa.Application.UseCases.Sites.ChangeSiteStatus;

public enum ChangeSiteStatusError
{
    NotFound,
}

public sealed class ChangeSiteStatusResult
{
    public bool IsSuccess { get; }
    public ChangeSiteStatusError? Error { get; }

    private ChangeSiteStatusResult(bool isSuccess, ChangeSiteStatusError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ChangeSiteStatusResult Success() => new(true, null);

    public static ChangeSiteStatusResult Failure(ChangeSiteStatusError error) => new(false, error);
}
