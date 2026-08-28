namespace Procofa.Application.UseCases.Users.GetUser;

public enum GetUserError
{
    /// <summary>No existe, o no pertenece al tenant actual (respuesta idéntica en ambos casos: 404).</summary>
    NotFound,
}

/// <summary>Un acceso a cliente concedido, tal como lo expone el detalle de usuario.</summary>
public sealed record UserClientAccessItem(Guid ClientId);

/// <summary>Resultado de <see cref="GetUserQueryHandler"/> — construido únicamente vía <see cref="Success"/>/<see cref="NotFound"/>. Nunca expone <c>password_hash</c>, refresh tokens ni password reset tokens.</summary>
public sealed class GetUserResult
{
    public bool IsSuccess { get; }
    public GetUserError? Error { get; }
    public Guid Id { get; }
    public string Email { get; } = string.Empty;
    public string FirstName { get; } = string.Empty;
    public string LastName { get; } = string.Empty;
    public string? Phone { get; }
    public bool IsActive { get; }
    public bool MustChangePassword { get; }
    public int FailedLoginAttempts { get; }
    public DateTime? LockedUntilUtc { get; }
    public DateTime? LastLoginAtUtc { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; }
    public IReadOnlyCollection<string> Roles { get; } = [];
    public IReadOnlyCollection<UserClientAccessItem> ClientAccess { get; } = [];

    private GetUserResult(bool isSuccess, GetUserError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    private GetUserResult(
        Guid id, string email, string firstName, string lastName, string? phone, bool isActive,
        bool mustChangePassword, int failedLoginAttempts, DateTime? lockedUntilUtc, DateTime? lastLoginAtUtc,
        DateTime createdAtUtc, DateTime updatedAtUtc, IReadOnlyCollection<string> roles,
        IReadOnlyCollection<UserClientAccessItem> clientAccess)
        : this(true, null)
    {
        Id = id;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        IsActive = isActive;
        MustChangePassword = mustChangePassword;
        FailedLoginAttempts = failedLoginAttempts;
        LockedUntilUtc = lockedUntilUtc;
        LastLoginAtUtc = lastLoginAtUtc;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Roles = roles;
        ClientAccess = clientAccess;
    }

    public static GetUserResult Success(
        Guid id, string email, string firstName, string lastName, string? phone, bool isActive,
        bool mustChangePassword, int failedLoginAttempts, DateTime? lockedUntilUtc, DateTime? lastLoginAtUtc,
        DateTime createdAtUtc, DateTime updatedAtUtc, IReadOnlyCollection<string> roles,
        IReadOnlyCollection<UserClientAccessItem> clientAccess) =>
        new(id, email, firstName, lastName, phone, isActive, mustChangePassword, failedLoginAttempts,
            lockedUntilUtc, lastLoginAtUtc, createdAtUtc, updatedAtUtc, roles, clientAccess);

    public static GetUserResult NotFound() => new(false, GetUserError.NotFound);
}
