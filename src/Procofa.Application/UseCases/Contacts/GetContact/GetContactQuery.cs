namespace Procofa.Application.UseCases.Contacts.GetContact;

public sealed record GetContactQuery(Guid ClientId, Guid ContactId);
