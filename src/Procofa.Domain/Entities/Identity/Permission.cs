namespace Procofa.Domain.Entities.Identity;

/// <summary>
/// Permiso atómico del sistema (ej. <c>AUDITS_CREATE</c>, <c>REPORTS_VALIDATE</c>).
/// Tabla física: <c>permissions</c> — catálogo global sin <c>tenant_id</c>,
/// sin RLS, solo lectura para <c>procofa_app</c>. 17 filas sembradas.
/// Identidad semántica estable: <see cref="Code"/> (ver <see cref="Role"/>).
/// </summary>
public sealed class Permission
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    private Permission() { }

    public Permission(Guid id, string code, string name, string? description)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
    }
}
