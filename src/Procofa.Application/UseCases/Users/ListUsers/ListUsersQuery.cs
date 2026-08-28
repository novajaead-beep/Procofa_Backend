namespace Procofa.Application.UseCases.Users.ListUsers;

/// <summary><c>GET /api/users</c>. <see cref="Page"/>/<see cref="PageSize"/> llegan ya con sus
/// defaults aplicados (1/25) por Api — el handler solo clampa <see cref="PageSize"/> a un máximo de
/// 100 (defensa en profundidad, nunca confía únicamente en el default del binding). </summary>
public sealed record ListUsersQuery(
    string? Search,
    bool? IsActive,
    string? Role,
    int Page,
    int PageSize);
