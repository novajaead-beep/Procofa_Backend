using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Domain.Entities.Identity;

/// <summary>
/// Rol de sistema (ADMIN, AUDITOR_LIDER, AUDITOR_APOYO, CLIENTE, CONSULTOR).
/// Tabla física: <c>roles</c> — catálogo global SIN <c>tenant_id</c>, sin RLS,
/// controlado por despliegue (<c>procofa_app</c> solo tiene GRANT SELECT).
///
/// Identidad semántica estable: <see cref="Code"/> — nunca hardcodear el
/// UUID en código (decisión congelada #5, baseline V2.1). Resolución en
/// runtime por <c>code</c> vía un catálogo cacheado en memoria
/// (<c>ICatalogLookup&lt;Role&gt;</c>, a implementar en Application/Infrastructure
/// en una instrucción futura — no en esta).
///
/// Posee <see cref="Permissions"/> (tabla física <c>role_permissions</c>,
/// PK compuesta <c>(role_id, permission_id)</c> sin columna <c>id</c> propia
/// → colección owned, sin <c>DbSet</c> propio).
/// </summary>
public sealed class Role
{
    private readonly List<RolePermission> _permissions = [];

    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role() { }

    public Role(Guid id, string code, string name, string? description)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
    }
}
