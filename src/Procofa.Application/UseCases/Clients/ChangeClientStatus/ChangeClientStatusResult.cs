namespace Procofa.Application.UseCases.Clients.ChangeClientStatus;

public enum ChangeClientStatusError
{
    NotFound,
}

public sealed class ChangeClientStatusResult
{
    public bool IsSuccess { get; }
    public ChangeClientStatusError? Error { get; }

    private ChangeClientStatusResult(bool isSuccess, ChangeClientStatusError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ChangeClientStatusResult Success() => new(true, null);

    public static ChangeClientStatusResult Failure(ChangeClientStatusError error) => new(false, error);
}
