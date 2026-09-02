using Procofa.Application.Abstractions.Checklists;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.UseCases.Checklists;

/// <summary>Resultado de <see cref="ChecklistPublishedResolver.ResolveAsync"/> y <see
/// cref="ChecklistPublishedResolver.TryResolveExactAsync"/>: el checklist y su última versión
/// PUBLISHED, junto con si la coincidencia fue exacta por <c>AuditTypeId</c> o cayó al genérico
/// (fallback).</summary>
public sealed record ChecklistPublishedResolution(Checklist Checklist, ChecklistVersion Version, bool IsExactMatch);

/// <summary>Resolución compartida de checklist aplicable por Program + Profile + AuditType
/// opcional, usada tanto por <c>ResolveChecklistQueryHandler</c> (lectura de plantilla) como por
/// <c>ReplaceAuditChecklistsCommandHandler</c> (validación de que un genérico no desplace a un
/// exacto disponible). No hay UNIQUE de BD sobre (program_id, profile_id, audit_type_id) en
/// <c>checklists</c> — por eso se prueba cada candidato activo, en orden determinístico, hasta
/// encontrar uno con versión PUBLISHED, en vez de asumir que el primer candidato sirve.</summary>
public static class ChecklistPublishedResolver
{
    public static async Task<ChecklistPublishedResolution?> ResolveAsync(
        IChecklistRepository checklistRepository, IChecklistVersionRepository checklistVersionRepository,
        Guid tenantId, Guid programId, Guid profileId, Guid? auditTypeId, CancellationToken cancellationToken)
    {
        if (auditTypeId.HasValue)
        {
            var exact = await TryResolveAsync(
                checklistRepository, checklistVersionRepository, tenantId, programId, profileId, auditTypeId,
                cancellationToken);
            if (exact is not null)
            {
                return exact;
            }
        }

        var fallback = await TryResolveAsync(
            checklistRepository, checklistVersionRepository, tenantId, programId, profileId, null,
            cancellationToken);
        return fallback;
    }

    /// <summary>Igual que la rama "exacta" de <see cref="ResolveAsync"/>, sin caer al genérico —
    /// usado por <c>ReplaceAuditChecklistsCommandHandler</c> para saber si existe un exacto
    /// publicado disponible, sin resolver también el fallback (el handler ya tiene su propio
    /// checklist elegido explícitamente por id).</summary>
    public static Task<ChecklistPublishedResolution?> TryResolveExactAsync(
        IChecklistRepository checklistRepository, IChecklistVersionRepository checklistVersionRepository,
        Guid tenantId, Guid programId, Guid profileId, Guid auditTypeId, CancellationToken cancellationToken) =>
        TryResolveAsync(
            checklistRepository, checklistVersionRepository, tenantId, programId, profileId, auditTypeId,
            cancellationToken);

    private static async Task<ChecklistPublishedResolution?> TryResolveAsync(
        IChecklistRepository checklistRepository, IChecklistVersionRepository checklistVersionRepository,
        Guid tenantId, Guid programId, Guid profileId, Guid? auditTypeId, CancellationToken cancellationToken)
    {
        var candidates = await checklistRepository.ListActiveCandidatesAsync(
            tenantId, programId, profileId, auditTypeId, cancellationToken);

        foreach (var candidate in candidates)
        {
            var version = await checklistVersionRepository.GetLatestPublishedAsync(
                tenantId, candidate.Id, cancellationToken);
            if (version is not null)
            {
                return new ChecklistPublishedResolution(candidate, version, auditTypeId.HasValue);
            }
        }

        return null;
    }
}
