namespace Procofa.Api.Contracts.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    IReadOnlyCollection<string> Roles);
