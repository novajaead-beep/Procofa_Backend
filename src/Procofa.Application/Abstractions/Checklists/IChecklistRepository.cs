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

    /// <summary>Candidato para <c>GET /api/checklists/resolve</c>: checklist activo que coincide
    /// exactamente con (program, profile, auditTypeId) — <paramref name="auditTypeId"/> nulo busca
    /// el genérico (<c>audit_type_id IS NULL</c>), nunca "cualquiera". Si el baseline permite más de
    /// un checklist activo bajo la misma combinación, se resuelve determinísticamente por el más
    /// reciente (<c>created_at_utc</c> desc, <c>id</c> como desempate).</summary>
    Task<Checklist?> FindActiveForResolutionAsync(
        Guid tenantId, Guid programId, Guid profileId, Guid? auditTypeId, CancellationToken cancellationToken);
}
