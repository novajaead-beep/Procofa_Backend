namespace Procofa.Application.UseCases.Contacts.UpdateContact;

public enum UpdateContactError
{
    NotFound,
    ValidationFailed,
    CompanyNotFound,
}

public sealed class UpdateContactResult
{
    public bool IsSuccess { get; }
    public UpdateContactError? Error { get; }
    public string? ErrorDetail { get; }

    private UpdateContactResult(bool isSuccess, UpdateContactError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static UpdateContactResult Success() => new(true, null, null);

    public static UpdateContactResult Failure(UpdateContactError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
