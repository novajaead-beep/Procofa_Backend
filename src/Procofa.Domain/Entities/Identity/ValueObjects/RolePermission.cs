namespace Procofa.Domain.Entities.Identity.ValueObjects;

/// <summary>
/// Asignación de un <see cref="Permission"/> a un <see cref="Role"/>.
/// Tabla física: <c>role_permissions</c> — PK compuesta
/// <c>(role_id, permission_id)</c>, sin columna <c>id</c>, sin <c>tenant_id</c>
/// (catálogo global). Mapeada como colección owned dentro de
/// <see cref="Role.Permissions"/>, sin <c>DbSet</c> propio (Instrucción 03,
/// sección "PKs compuestas sin columna id").
/// </summary>
public sealed class RolePermission
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }

    private RolePermission() { }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}
