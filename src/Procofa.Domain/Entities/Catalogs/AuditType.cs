namespace Procofa.Domain.Entities.Catalogs;

/// <summary>
/// Tipo de auditoría (INTERNA_OEA, INTERNA_CTPAT, SOCIO_COMERCIAL,
/// DOCUMENTAL, EN_SITIO, SEGUIMIENTO). Catálogo global sin <c>tenant_id</c>,
/// sin RLS. Tabla física: <c>audit_types</c>, 6 filas sembradas. Mismo
/// régimen de GRANTs que <see cref="Program"/> (decisión congelada #9).
/// <c>checklists.audit_type_id IS NULL</c> = checklist genérico para
/// (Program, Profile) — ver <c>ChecklistConfiguration</c>.
/// </summary>
public sealed class AuditType
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private AuditType() { }

    public AuditType(Guid id, string code, string name, string? description)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
        IsActive = true;
    }
}
