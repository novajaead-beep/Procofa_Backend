using Procofa.Application.Abstractions.Identity;

namespace Procofa.Application.UseCases.Auth.Login;

/// <summary>
/// Resultado de <see cref="LoginCommandHandler"/>. Construido únicamente vía <see
/// cref="Success"/>/<see cref="Failure"/> — nunca con un constructor público — para que sea
/// imposible representar un estado inconsistente (ej. éxito sin token, o fallo con token).
/// </summary>
public sealed class LoginResult
{
    public bool IsSuccess { get; }
    public LoginError? Error { get; }
    public Guid? UserId { get; }
    public IReadOnlyCollection<string> Roles { get; }
    public AccessToken? AccessToken { get; }
    public string? RefreshToken { get; }
    public DateTime? RefreshTokenExpiresAtUtc { get; }

    private LoginResult(
        bool isSuccess,
        LoginError? error,
        Guid? userId,
        IReadOnlyCollection<string> roles,
        AccessToken? accessToken,
        string? refreshToken,
        DateTime? refreshTokenExpiresAtUtc)
    {
        IsSuccess = isSuccess;
        Error = error;
        UserId = userId;
        Roles = roles;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
    }

    public static LoginResult Success(
        Guid userId,
        IReadOnlyCollection<string> roles,
        AccessToken accessToken,
        string refreshToken,
        DateTime refreshTokenExpiresAtUtc) =>
        new(true, null, userId, roles, accessToken, refreshToken, refreshTokenExpiresAtUtc);

    public static LoginResult Failure(LoginError error) =>
        new(false, error, null, [], null, null, null);
}
