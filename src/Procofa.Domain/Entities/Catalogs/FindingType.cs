namespace Procofa.Domain.Entities.Catalogs;

/// <summary>
/// Clasificación de un <c>Finding</c> (NO_CONFORMIDAD, OBSERVACION,
/// OPORTUNIDAD_MEJORA). Catálogo global sin <c>tenant_id</c>, sin RLS, solo
/// lectura para <c>procofa_app</c>. Tabla física: <c>finding_types</c>,
/// 3 filas sembradas. A diferencia de otros catálogos de este grupo, NO
/// tiene columna <c>is_active</c> — fidelidad física, no se agrega en EF.
/// </summary>
public sealed class FindingType
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }

    private FindingType() { }

    public FindingType(Guid id, string code, string name, string? description)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
    }
}
