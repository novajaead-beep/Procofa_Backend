namespace Procofa.Api.Contracts.Auth;

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? Phone,
    bool MustChangePassword,
    IReadOnlyCollection<string> Roles);
