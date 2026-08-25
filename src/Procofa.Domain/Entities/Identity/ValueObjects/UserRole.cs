namespace Procofa.Domain.Entities.Identity.ValueObjects;

/// <summary>
/// Asignación de un <c>Role</c> de sistema a un <see cref="User"/>.
/// Tabla física: <c>user_roles</c> — PK compuesta <c>(user_id, role_id)</c>,
/// tenant-scoped, sin columna <c>id</c>. Colección owned dentro de
/// <see cref="User.Roles"/>, sin <c>DbSet</c> propio.
/// </summary>
public sealed class UserRole
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private UserRole() { }

    public UserRole(Guid tenantId, Guid userId, Guid roleId, Guid? assignedByUserId)
    {
        TenantId = tenantId;
        UserId = userId;
        RoleId = roleId;
        AssignedByUserId = assignedByUserId;
    }
}
