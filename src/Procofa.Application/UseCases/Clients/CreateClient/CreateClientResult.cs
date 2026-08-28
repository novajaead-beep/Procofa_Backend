namespace Procofa.Application.UseCases.Clients.CreateClient;

public enum CreateClientError
{
    ValidationFailed,
    TaxIdAlreadyExists,
    ProgramNotFound,
}

public sealed class CreateClientResult
{
    public bool IsSuccess { get; }
    public CreateClientError? Error { get; }
    public string? ErrorDetail { get; }
    public Guid? ClientId { get; }

    private CreateClientResult(bool isSuccess, CreateClientError? error, string? errorDetail, Guid? clientId)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
        ClientId = clientId;
    }

    public static CreateClientResult Success(Guid clientId) => new(true, null, null, clientId);

    public static CreateClientResult Failure(CreateClientError error, string? errorDetail = null) =>
        new(false, error, errorDetail, null);
}
