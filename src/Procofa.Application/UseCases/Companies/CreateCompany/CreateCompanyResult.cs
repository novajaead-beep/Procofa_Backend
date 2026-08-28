namespace Procofa.Application.UseCases.Companies.CreateCompany;

public enum CreateCompanyError
{
    ClientNotFound,
    ValidationFailed,
    TaxIdAlreadyExists,
}

public sealed class CreateCompanyResult
{
    public bool IsSuccess { get; }
    public CreateCompanyError? Error { get; }
    public string? ErrorDetail { get; }
    public Guid? CompanyId { get; }

    private CreateCompanyResult(bool isSuccess, CreateCompanyError? error, string? errorDetail, Guid? companyId)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
        CompanyId = companyId;
    }

    public static CreateCompanyResult Success(Guid companyId) => new(true, null, null, companyId);

    public static CreateCompanyResult Failure(CreateCompanyError error, string? errorDetail = null) =>
        new(false, error, errorDetail, null);
}
