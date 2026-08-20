using Procofa.Domain.Audits;

namespace Procofa.Application.Ports.Out;

/// <summary>
/// Puerto de salida de persistencia para el contexto de Auditorías. Expone operaciones separadas
/// para AuditPlan (aggregate root con checklist), AuditResult (escritura de alta frecuencia —
/// autosave) y Finding, evitando forzar la carga del aggregate completo en la ruta caliente de
/// ejecución. SaveChangesAsync actúa como Unit of Work explícito para las transacciones
/// multi-entidad descritas en la Sección 5.1 del SRS (ej. Finding + AuditResult en una sola operación).
/// </summary>
public interface IAuditRepository
{
    // --- AuditPlan ---
    Task<AuditPlan?> GetPlanByIdAsync(Guid auditPlanId, CancellationToken cancellationToken);

    /// <summary>Carga el AuditPlan junto con su Checklist (CriterionSnapshot) — requerido para Start()/Close().</summary>
    Task<AuditPlan?> GetPlanWithChecklistAsync(Guid auditPlanId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AuditPlan>> GetPlansByClientAsync(Guid clientId, CancellationToken cancellationToken);
    Task AddPlanAsync(AuditPlan auditPlan, CancellationToken cancellationToken);

    // --- AuditResult (ruta caliente: autosave HU-09) ---
    Task<AuditResult?> GetResultByCriterionAsync(Guid criterionSnapshotId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<AuditResult>> GetResultsByPlanAsync(Guid auditPlanId, CancellationToken cancellationToken);

    /// <summary>Upsert idempotente de un resultado individual — transacción atómica por criterio (5.1 SRS).</summary>
    Task SaveResultAsync(AuditResult result, CancellationToken cancellationToken);

    // --- Finding ---
    Task<Finding?> GetFindingByIdAsync(Guid findingId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Finding>> GetFindingsByPlanAsync(Guid auditPlanId, CancellationToken cancellationToken);
    Task AddFindingAsync(Finding finding, CancellationToken cancellationToken);

    /// <summary>
    /// Confirma la unidad de trabajo actual. Usado, por ejemplo, al generar un Finding a partir de
    /// un AuditResult: ambas entidades se adjuntan al contexto y se confirman juntas o se revierten
    /// juntas ante cualquier fallo (ver Sección 5.1 del SRS).
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
