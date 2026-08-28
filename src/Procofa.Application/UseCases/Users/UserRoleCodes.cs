namespace Procofa.Application.UseCases.Users;

/// <summary>
/// lista cerrada de roles válidos para el módulo de gestión de usuarios — "No inventar roles
/// nuevos". Defensa en profundidad ANTES de tocar la BD: <see
/// cref="Procofa.Domain.Entities.Identity.Role"/> sigue siendo la autoridad real (catálogo en
/// <c>roles</c>, resuelto vía <c>IRoleRepository</c>), pero validar contra esta lista primero evita
/// una query innecesaria para un código que ni siquiera está en el conjunto permitido, y da un
/// mensaje de error más claro que "rol no encontrado". </summary>
public static class UserRoleCodes
{
    public const string Admin = "ADMIN";
    public const string AuditorLider = "AUDITOR_LIDER";
    public const string AuditorApoyo = "AUDITOR_APOYO";
    public const string Cliente = "CLIENTE";
    public const string Consultor = "CONSULTOR";

    public static readonly IReadOnlyCollection<string> All =
    [
        Admin,
        AuditorLider,
        AuditorApoyo,
        Cliente,
        Consultor,
    ];
}
