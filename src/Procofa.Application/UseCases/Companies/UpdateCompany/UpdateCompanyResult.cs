namespace Procofa.Application.UseCases.Companies.UpdateCompany;

public enum UpdateCompanyError
{
    NotFound,
    ValidationFailed,
    TaxIdAlreadyExists,
}

public sealed class UpdateCompanyResult
{
    public bool IsSuccess { get; }
    public UpdateCompanyError? Error { get; }
    public string? ErrorDetail { get; }

    private UpdateCompanyResult(bool isSuccess, UpdateCompanyError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static UpdateCompanyResult Success() => new(true, null, null);

    public static UpdateCompanyResult Failure(UpdateCompanyError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
