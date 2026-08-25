namespace Procofa.Domain.Entities.Catalogs;

/// <summary>
/// Prioridad de un <c>Finding</c> (ALTA, MEDIA, BAJA). Catálogo global sin
/// <c>tenant_id</c>, sin RLS, solo lectura para <c>procofa_app</c>. Tabla
/// física: <c>finding_priorities</c>, 3 filas sembradas. Sin
/// <c>description</c> ni <c>is_active</c> — fidelidad física, la tabla más
/// minimalista del grupo de catálogos.
/// </summary>
public sealed class FindingPriority
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int SortOrder { get; private set; }

    private FindingPriority() { }

    public FindingPriority(Guid id, string code, string name, int sortOrder)
    {
        Id = id;
        Code = code;
        Name = name;
        SortOrder = sortOrder;
    }
}
