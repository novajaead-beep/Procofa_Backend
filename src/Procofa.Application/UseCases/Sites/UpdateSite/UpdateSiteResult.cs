namespace Procofa.Application.UseCases.Sites.UpdateSite;

public enum UpdateSiteError
{
    NotFound,
    ValidationFailed,
}

public sealed class UpdateSiteResult
{
    public bool IsSuccess { get; }
    public UpdateSiteError? Error { get; }
    public string? ErrorDetail { get; }

    private UpdateSiteResult(bool isSuccess, UpdateSiteError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static UpdateSiteResult Success() => new(true, null, null);

    public static UpdateSiteResult Failure(UpdateSiteError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
