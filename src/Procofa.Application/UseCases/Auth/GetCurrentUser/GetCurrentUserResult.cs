namespace Procofa.Application.UseCases.Auth.GetCurrentUser;

public sealed class GetCurrentUserResult
{
    public bool IsSuccess { get; }

    public Guid Id { get; }

    public string Email { get; } = string.Empty;

    public string FirstName { get; } = string.Empty;

    public string LastName { get; } = string.Empty;

    public string? Phone { get; }

    public bool MustChangePassword { get; }

    public IReadOnlyCollection<string> Roles { get; } = [];

    private GetCurrentUserResult(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    private GetCurrentUserResult(
        Guid id,
        string email,
        string firstName,
        string lastName,
        string? phone,
        bool mustChangePassword,
        IReadOnlyCollection<string> roles)
        : this(true)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        MustChangePassword = mustChangePassword;
        Roles = roles;
    }

    public static GetCurrentUserResult Success(
        Guid id,
        string email,
        string firstName,
        string lastName,
        string? phone,
        bool mustChangePassword,
        IReadOnlyCollection<string> roles)
    {
        return new GetCurrentUserResult(
            id,
            email,
            firstName,
            lastName,
            phone,
            mustChangePassword,
            roles);
    }

    public static GetCurrentUserResult NotFound()
    {
        return new GetCurrentUserResult(false);
    }
}
