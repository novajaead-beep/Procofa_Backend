namespace Procofa.Api.Contracts.Clients;

/// <summary>Body compartido por los 4 endpoints <c>PATCH .../status</c> del módulo de clientes
/// (client, company, site, contact) — mismo shape, un solo tipo.</summary>
public sealed record ChangeStatusRequest(bool IsActive);
