namespace Procofa.Application.UseCases.Contacts.GetContact;

public enum GetContactError
{
    NotFound,
}

public sealed class GetContactResult
{
    public bool IsSuccess { get; }
    public GetContactError? Error { get; }
    public Guid Id { get; }
    public Guid ClientId { get; }
    public Guid? AuditedCompanyId { get; }
    public string FirstName { get; } = string.Empty;
    public string LastName { get; } = string.Empty;
    public string? JobTitle { get; }
    public string? Email { get; }
    public string? Phone { get; }
    public bool IsActive { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; }

    private GetContactResult(bool isSuccess, GetContactError? error) { IsSuccess = isSuccess; Error = error; }

    private GetContactResult(
        Guid id, Guid clientId, Guid? auditedCompanyId, string firstName, string lastName, string? jobTitle,
        string? email, string? phone, bool isActive, DateTime createdAtUtc, DateTime updatedAtUtc)
        : this(true, null)
    {
        Id = id;
        ClientId = clientId;
        AuditedCompanyId = auditedCompanyId;
        FirstName = firstName;
        LastName = lastName;
        JobTitle = jobTitle;
        Email = email;
        Phone = phone;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GetContactResult Success(
        Guid id, Guid clientId, Guid? auditedCompanyId, string firstName, string lastName, string? jobTitle,
        string? email, string? phone, bool isActive, DateTime createdAtUtc, DateTime updatedAtUtc) =>
        new(id, clientId, auditedCompanyId, firstName, lastName, jobTitle, email, phone, isActive, createdAtUtc,
            updatedAtUtc);

    public static GetContactResult NotFound() => new(false, GetContactError.NotFound);
}
