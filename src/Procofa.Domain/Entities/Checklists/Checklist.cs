namespace Procofa.Domain.Entities.Checklists;

/// <summary>
/// Encabezado/familia de checklist (Program, Profile, AuditType? opcional).
/// Aggregate Root — responsabilidad limitada a los metadatos del
/// encabezado; el contenido evaluable vive en <see cref="ChecklistVersion"/>
/// (dos aggregate roots distintos, baseline V2.1 sección F).
/// Tabla física: <c>checklists</c>, tenant-scoped, RLS+FORCE RLS.
///
/// <c>AuditTypeId IS NULL</c> = checklist genérico para (Program, Profile).
/// La resolución en <c>CreateAudit</c> (futuro) prioriza coincidencia exacta
/// por <c>AuditTypeId</c>; si no hay match exacto, usa la versión con
/// <c>AuditTypeId IS NULL</c> como fallback (decisión congelada #10).
/// </summary>
public sealed class Checklist
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ProgramId { get; private set; }
    public Guid ProfileId { get; private set; }
    public Guid? AuditTypeId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_checklists_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private Checklist() { }

    public Checklist(
        Guid id,
        Guid tenantId,
        Guid programId,
        Guid profileId,
        Guid? auditTypeId,
        string name,
        string? description,
        Guid createdByUserId)
    {
        Id = id;
        TenantId = tenantId;
        ProgramId = programId;
        ProfileId = profileId;
        AuditTypeId = auditTypeId;
        Name = name;
        Description = description;
        CreatedByUserId = createdByUserId;
        IsActive = true;
    }
}
