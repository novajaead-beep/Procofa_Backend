namespace Procofa.Application.UseCases.Clients.UpdateClient;

public enum UpdateClientError
{
    NotFound,
    ValidationFailed,
    TaxIdAlreadyExists,
    ProgramNotFound,
}

public sealed class UpdateClientResult
{
    public bool IsSuccess { get; }
    public UpdateClientError? Error { get; }
    public string? ErrorDetail { get; }

    private UpdateClientResult(bool isSuccess, UpdateClientError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static UpdateClientResult Success() => new(true, null, null);

    public static UpdateClientResult Failure(UpdateClientError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
