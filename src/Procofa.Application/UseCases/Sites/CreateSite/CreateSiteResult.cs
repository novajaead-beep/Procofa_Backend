namespace Procofa.Application.UseCases.Sites.CreateSite;

public enum CreateSiteError
{
    CompanyNotFound,
    ValidationFailed,
}

public sealed class CreateSiteResult
{
    public bool IsSuccess { get; }
    public CreateSiteError? Error { get; }
    public string? ErrorDetail { get; }
    public Guid? SiteId { get; }

    private CreateSiteResult(bool isSuccess, CreateSiteError? error, string? errorDetail, Guid? siteId)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
        SiteId = siteId;
    }

    public static CreateSiteResult Success(Guid siteId) => new(true, null, null, siteId);

    public static CreateSiteResult Failure(CreateSiteError error, string? errorDetail = null) =>
        new(false, error, errorDetail, null);
}
