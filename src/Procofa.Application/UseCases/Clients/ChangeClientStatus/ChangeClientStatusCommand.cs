namespace Procofa.Application.UseCases.Clients.ChangeClientStatus;

/// <summary><c>PATCH /api/clients/{clientId}/status</c>.</summary>
public sealed record ChangeClientStatusCommand(Guid ClientId, bool IsActive);
