namespace Procofa.Application.UseCases.Contacts.CreateContact;

/// <summary><c>POST /api/clients/{clientId}/contacts</c>. <see cref="ClientId"/> viene siempre de
/// la ruta.</summary>
public sealed record CreateContactCommand(
    Guid ClientId,
    Guid? AuditedCompanyId,
    string? FirstName,
    string? LastName,
    string? JobTitle,
    string? Email,
    string? Phone);
