namespace Procofa.Application.UseCases.Contacts.ChangeContactStatus;

public enum ChangeContactStatusError
{
    NotFound,
}

public sealed class ChangeContactStatusResult
{
    public bool IsSuccess { get; }
    public ChangeContactStatusError? Error { get; }

    private ChangeContactStatusResult(bool isSuccess, ChangeContactStatusError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ChangeContactStatusResult Success() => new(true, null);

    public static ChangeContactStatusResult Failure(ChangeContactStatusError error) => new(false, error);
}
