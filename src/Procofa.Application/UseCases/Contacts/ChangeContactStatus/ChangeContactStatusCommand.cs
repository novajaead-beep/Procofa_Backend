namespace Procofa.Application.UseCases.Contacts.ChangeContactStatus;

public sealed record ChangeContactStatusCommand(Guid ClientId, Guid ContactId, bool IsActive);
