namespace Procofa.Application.UseCases.Contacts.CreateContact;

public enum CreateContactError
{
    ClientNotFound,
    ValidationFailed,
    CompanyNotFound,
}

public sealed class CreateContactResult
{
    public bool IsSuccess { get; }
    public CreateContactError? Error { get; }
    public string? ErrorDetail { get; }
    public Guid? ContactId { get; }

    private CreateContactResult(bool isSuccess, CreateContactError? error, string? errorDetail, Guid? contactId)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
        ContactId = contactId;
    }

    public static CreateContactResult Success(Guid contactId) => new(true, null, null, contactId);

    public static CreateContactResult Failure(CreateContactError error, string? errorDetail = null) =>
        new(false, error, errorDetail, null);
}
