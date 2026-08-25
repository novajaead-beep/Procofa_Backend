namespace Procofa.Domain.Entities.Catalogs;

/// <summary>
/// Estado del ciclo de vida de un <c>Finding</c> (ABIERTO, EN_PROCESO,
/// PENDIENTE_VALIDACION, CERRADO*, RECHAZADO — *terminal vía
/// <see cref="IsClosed"/>). Catálogo global sin <c>tenant_id</c>, sin RLS,
/// solo lectura para <c>procofa_app</c>. Tabla física:
/// <c>finding_statuses</c>, 5 filas sembradas.
/// </summary>
public sealed class FindingStatus
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsClosed { get; private set; }
    public int SortOrder { get; private set; }

    private FindingStatus() { }

    public FindingStatus(Guid id, string code, string name, bool isClosed, int sortOrder)
    {
        Id = id;
        Code = code;
        Name = name;
        IsClosed = isClosed;
        SortOrder = sortOrder;
    }
}
