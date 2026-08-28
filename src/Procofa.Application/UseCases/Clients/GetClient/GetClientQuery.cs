namespace Procofa.Application.UseCases.Clients.GetClient;

/// <summary><c>GET /api/clients/{clientId}</c>.</summary>
public sealed record GetClientQuery(Guid ClientId);
