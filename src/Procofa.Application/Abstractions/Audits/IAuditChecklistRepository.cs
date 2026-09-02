using Procofa.Domain.Entities.Audits;

namespace Procofa.Application.Abstractions.Audits;

/// <summary>Puerto de acceso a <see cref="AuditChecklist"/> — entidad independiente con
/// <c>DbSet</c> propio (referenciada por <c>audit_criteria.audit_checklist_id</c>), fuera de la
/// colección owned de <see cref="Audit"/>.</summary>
/// <summary><see cref="AuditChecklist"/> resuelto con los metadatos del <c>Checklist</c>/<c>
/// ChecklistVersion</c> a los que apunta — <see cref="AuditChecklist"/> solo guarda <c>
/// ChecklistVersionId</c>, así que saber a qué Program/Profile/AuditType pertenece requiere este
/// join. Usado por <c>GetAuditQueryHandler</c> (exponer los checklists asignados) y por
/// <c>UpdateAuditCommandHandler</c>/<c>ReplaceAuditProgramsCommandHandler</c> (proteger checklists
/// ya asignados de quedar incompatibles con un cambio de Profile/AuditType/Programs).</summary>
public sealed record AuditChecklistDetail(
    Guid AuditChecklistId,
    Guid ChecklistId,
    Guid ChecklistVersionId,
    int VersionNumber,
    string ChecklistName,
    Guid ProgramId,
    Guid ProfileId,
    Guid? AuditTypeId,
    DateTime AssignedAtUtc);

public interface IAuditChecklistRepository
{
    Task<IReadOnlyList<AuditChecklist>> ListByAuditAsync(
        Guid tenantId, Guid auditId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditChecklistDetail>> ListDetailedByAuditAsync(
        Guid tenantId, Guid auditId, CancellationToken cancellationToken);

    /// <summary>Reemplazo transaccional completo: borra las filas existentes de
    /// <c>audit_checklists</c> para <paramref name="auditId"/> y agrega <paramref
    /// name="newChecklists"/> — misma semántica que <see cref="Audit.ReplacePrograms"/>, aplicada
    /// a una entidad con <c>DbSet</c> propio en vez de una colección owned.</summary>
    Task ReplaceAsync(
        Guid tenantId, Guid auditId, IReadOnlyCollection<AuditChecklist> newChecklists,
        CancellationToken cancellationToken);
}
