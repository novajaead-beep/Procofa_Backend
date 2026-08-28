namespace Procofa.Application.UseCases.Companies.ChangeCompanyStatus;

public enum ChangeCompanyStatusError
{
    NotFound,
}

public sealed class ChangeCompanyStatusResult
{
    public bool IsSuccess { get; }
    public ChangeCompanyStatusError? Error { get; }

    private ChangeCompanyStatusResult(bool isSuccess, ChangeCompanyStatusError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ChangeCompanyStatusResult Success() => new(true, null);

    public static ChangeCompanyStatusResult Failure(ChangeCompanyStatusError error) => new(false, error);
}
