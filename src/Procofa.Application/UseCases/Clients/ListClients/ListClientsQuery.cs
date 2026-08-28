namespace Procofa.Application.UseCases.Clients.ListClients;

/// <summary><c>GET /api/clients</c>. <see cref="Page"/>/<see cref="PageSize"/> llegan ya con sus
/// defaults aplicados (1/25) por Api — el handler solo clampa <see cref="PageSize"/> a un máximo de
/// 100.</summary>
public sealed record ListClientsQuery(
    string? Search,
    bool? IsActive,
    string? Program,
    int Page,
    int PageSize);
