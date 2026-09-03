namespace Procofa.Application.UseCases.Auth.Logout;

public sealed record LogoutCommand(
    string? RawRefreshToken,
    string? IpAddress,
    string? UserAgent);
