namespace Procofa.Application.UseCases.Auth.RefreshSession;

public sealed record RefreshSessionCommand(
    string RawRefreshToken);
