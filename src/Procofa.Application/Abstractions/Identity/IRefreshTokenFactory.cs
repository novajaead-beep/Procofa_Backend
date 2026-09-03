namespace Procofa.Application.Abstractions.Identity;

public sealed record GeneratedRefreshToken(
    string RawToken,
    string TokenHash);

public interface IRefreshTokenFactory
{
    GeneratedRefreshToken Create();

    string Hash(string rawToken);
}
