using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.Abstractions.Checklists;

public sealed record ChecklistListRow(
    Guid Id,
    Guid ProgramId,
    Guid ProfileId,
    Guid? AuditTypeId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record ChecklistListPageResult(IReadOnlyList<ChecklistListRow> Items, int Total);

public interface IChecklistRepository
{
    Task<Checklist?> GetByIdAsync(Guid tenantId, Guid checklistId, CancellationToken cancellationToken);

    Task AddAsync(Checklist checklist, CancellationToken cancellationToken);

    Task<ChecklistListPageResult> ListAsync(
        Guid tenantId,
        string? search,
        Guid? programId,
        Guid? profileId,
        Guid? auditTypeId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Candidatos para <c>GET /api/checklists/resolve</c> y para la resolución de
    /// checklists de auditoría: checklists activos que coinciden exactamente con (program, profile,
    /// auditTypeId) — <paramref name="auditTypeId"/> nulo busca el genérico (<c>audit_type_id IS
    /// NULL</c>), nunca "cualquiera". No hay UNIQUE de BD sobre (program, profile, audit_type_id):
    /// más de un checklist activo puede coincidir con la misma combinación, así que se devuelven
    /// TODOS los candidatos, ordenados determinísticamente (<c>created_at_utc</c> desc, <c>id</c>
    /// como desempate) — el llamador debe probarlos en ese orden hasta encontrar uno con versión
    /// PUBLISHED, nunca quedarse con el primero sin verificar.</summary>
    Task<IReadOnlyList<Checklist>> ListActiveCandidatesAsync(
        Guid tenantId, Guid programId, Guid profileId, Guid? auditTypeId, CancellationToken cancellationToken);
}
