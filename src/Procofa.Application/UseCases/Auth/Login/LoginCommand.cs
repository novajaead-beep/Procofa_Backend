namespace Procofa.Application.UseCases.Auth.Login;

/// <summary>
/// Comando de entrada para <see cref="LoginCommandHandler"/> (Instrucción 04).
/// Deliberadamente SIN <c>TenantId</c>: "el tenant nunca viene del request"
/// (sección "TENANT STAGE 1") — el handler lo resuelve exclusivamente desde
/// <c>ITenantContext</c>. <see cref="IpAddress"/>/<see cref="UserAgent"/> son
/// opcionales (metadatos de <c>access_logs</c>, resueltos por Api desde el
/// <c>HttpContext</c>) — su ausencia nunca bloquea el login.
/// </summary>
/// <param name="Email">Email tal como lo envía el cliente — la normalización (<see cref="Procofa.Domain.Entities.Identity.User.Normalize"/>) ocurre dentro del handler.</param>
/// <param name="Password">Contraseña en texto plano — nunca se loguea, nunca se persiste tal cual.</param>
/// <param name="IpAddress">Dirección IP del cliente, si está disponible.</param>
/// <param name="UserAgent">User-Agent del cliente, si está disponible.</param>
public sealed record LoginCommand(string Email, string Password, string? IpAddress, string? UserAgent);
