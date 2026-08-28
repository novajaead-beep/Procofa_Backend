namespace Procofa.Api.Contracts.Contacts;

public sealed record ContactListItemResponse(
    Guid Id, string FirstName, string LastName, string? JobTitle, string? Email, string? Phone, bool IsActive);

public sealed record ContactListResponse(IReadOnlyCollection<ContactListItemResponse> Items);

public sealed record ContactDetailResponse(
    Guid Id,
    Guid ClientId,
    Guid? AuditedCompanyId,
    string FirstName,
    string LastName,
    string? JobTitle,
    string? Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
