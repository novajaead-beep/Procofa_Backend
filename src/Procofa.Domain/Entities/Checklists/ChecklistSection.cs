namespace Procofa.Domain.Entities.Checklists;

/// <summary>
/// Sección dentro de una <see cref="ChecklistVersion"/>. Entidad
/// independiente con <c>DbSet</c> propio (ver justificación en
/// <see cref="ChecklistVersion"/>). Tabla física: <c>checklist_sections</c>,
/// tenant-scoped, RLS+FORCE RLS, <c>ON DELETE RESTRICT</c> desde
/// <c>checklist_versions</c> (protege el histórico).
/// Sin <c>created_at_utc</c>/<c>updated_at_utc</c> — fidelidad física, la
/// tabla no las tiene.
/// </summary>
public sealed class ChecklistSection
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ChecklistVersionId { get; private set; }
    public string? Code { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    private ChecklistSection() { }

    public ChecklistSection(
        Guid id,
        Guid tenantId,
        Guid checklistVersionId,
        string? code,
        string name,
        string? description,
        int sortOrder)
    {
        Id = id;
        TenantId = tenantId;
        ChecklistVersionId = checklistVersionId;
        Code = code;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
    }

    public void UpdateDetails(string? code, string name, string? description)
    {
        Code = code;
        Name = name;
        Description = description;
    }

    public void ChangeOrder(int sortOrder) => SortOrder = sortOrder;
}
