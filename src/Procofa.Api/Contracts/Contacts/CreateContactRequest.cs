namespace Procofa.Api.Contracts.Contacts;

public sealed record CreateContactRequest(
    Guid? AuditedCompanyId,
    string? FirstName,
    string? LastName,
    string? JobTitle,
    string? Email,
    string? Phone);

public sealed record CreateContactResponse(Guid Id);
