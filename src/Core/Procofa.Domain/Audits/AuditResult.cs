using Procofa.Domain.Audits.Enums;
using Procofa.Domain.Common;

namespace Procofa.Domain.Audits;

/// <summary>
/// Respuesta del auditor a un <see cref="CriterionSnapshot"/> (HU-08/HU-09). Aggregate independiente
/// de <see cref="AuditPlan"/> a propósito: es la entidad de mayor frecuencia de escritura del sistema
/// (autosave con debounce ≤3s por criterio activo, HU-09) y no debe requerir cargar el aggregate
/// completo del plan para persistirse.
/// </summary>
public sealed class AuditResult
{
    public const int MaxObservationsLength = 2000;

    public Guid Id { get; private set; }
    public Guid AuditPlanId { get; private set; }
    public Guid CriterionSnapshotId { get; private set; }
    public CriterionResultValue Value { get; private set; }
    public string? Observations { get; private set; }
    public Guid? AnsweredByUserId { get; private set; }
    public DateTime? AnsweredAtUtc { get; private set; }

    /// <summary>Idempotencia del autosave (5.2 SRS): descarta reintentos duplicados o fuera de orden.</summary>
    public Guid? LastOperationId { get; private set; }

    /// <summary>Token de concurrencia optimista mapeado a `xmin` de PostgreSQL (HU-11 / 5.2 SRS).</summary>
    public uint RowVersion { get; private set; }

    private AuditResult() { } // EF Core

    private AuditResult(Guid id, Guid auditPlanId, Guid criterionSnapshotId)
    {
        Id = id;
        AuditPlanId = auditPlanId;
        CriterionSnapshotId = criterionSnapshotId;
        Value = CriterionResultValue.NotAnswered;
    }

    /// <summary>Se crea en estado Pendiente al cargar el checklist (uno por CriterionSnapshot).</summary>
    public static AuditResult CreatePending(Guid auditPlanId, Guid criterionSnapshotId)
    {
        if (auditPlanId == Guid.Empty || criterionSnapshotId == Guid.Empty)
            throw new DomainException("AuditResult requiere un plan y un criterio válidos.");

        return new AuditResult(Guid.NewGuid(), auditPlanId, criterionSnapshotId);
    }

    /// <summary>
    /// HU-08/HU-09: registra la respuesta del auditor. Idempotente respecto a <paramref name="operationId"/>
    /// para tolerar reintentos del autosave ante conectividad intermitente sin duplicar ni desordenar escrituras.
    /// </summary>
    public void Answer(CriterionResultValue value, string? observations, Guid answeredByUserId, Guid operationId)
    {
        if (value == CriterionResultValue.NotAnswered)
            throw new DomainException("El resultado debe ser Cumple, No Cumple o No Aplica.");

        if (answeredByUserId == Guid.Empty)
            throw new DomainException("Se requiere el usuario que registra la respuesta.");

        if (LastOperationId == operationId)
            return; // Reintento idempotente ya aplicado: no-op.

        if (observations is { Length: > MaxObservationsLength })
            throw new DomainException($"Las observaciones exceden el máximo de {MaxObservationsLength} caracteres.");

        Value = value;
        Observations = observations;
        AnsweredByUserId = answeredByUserId;
        AnsweredAtUtc = DateTime.UtcNow;
        LastOperationId = operationId;
    }

    /// <summary>HU-16: solo un resultado en No Cumple habilita la generación de un hallazgo.</summary>
    public bool RequiresFinding() => Value == CriterionResultValue.NonCompliant;
}
