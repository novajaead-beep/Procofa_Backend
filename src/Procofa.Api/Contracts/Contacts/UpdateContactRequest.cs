namespace Procofa.Api.Contracts.Contacts;

public sealed record UpdateContactRequest(
    Guid? AuditedCompanyId,
    string? FirstName,
    string? LastName,
    string? JobTitle,
    string? Email,
    string? Phone);
