namespace Procofa.Application.UseCases.Contacts.UpdateContact;

public sealed record UpdateContactCommand(
    Guid ClientId,
    Guid ContactId,
    Guid? AuditedCompanyId,
    string? FirstName,
    string? LastName,
    string? JobTitle,
    string? Email,
    string? Phone);
