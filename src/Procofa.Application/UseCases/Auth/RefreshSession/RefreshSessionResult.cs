using Procofa.Application.Abstractions.Identity;

namespace Procofa.Application.UseCases.Auth.RefreshSession;

public enum RefreshSessionError
{
    InvalidRefreshToken
}

public sealed class RefreshSessionResult
{
    public bool IsSuccess { get; }

    public RefreshSessionError? Error { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public AccessToken? AccessToken { get; }

    public string? RefreshToken { get; }

    public DateTime? RefreshTokenExpiresAtUtc { get; }

    private RefreshSessionResult(
        bool isSuccess,
        RefreshSessionError? error,
        IReadOnlyCollection<string> roles,
        AccessToken? accessToken,
        string? refreshToken,
        DateTime? refreshTokenExpiresAtUtc)
    {
        IsSuccess = isSuccess;
        Error = error;
        Roles = roles;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
    }

    public static RefreshSessionResult Success(
        IReadOnlyCollection<string> roles,
        AccessToken accessToken,
        string refreshToken,
        DateTime refreshTokenExpiresAtUtc)
    {
        return new RefreshSessionResult(
            true,
            null,
            roles,
            accessToken,
            refreshToken,
            refreshTokenExpiresAtUtc);
    }

    public static RefreshSessionResult Failure()
    {
        return new RefreshSessionResult(
            false,
            RefreshSessionError.InvalidRefreshToken,
            [],
            null,
            null,
            null);
    }
}
