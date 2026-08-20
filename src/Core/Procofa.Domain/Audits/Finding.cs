using Procofa.Domain.Audits.Enums;
using Procofa.Domain.Common;

namespace Procofa.Domain.Audits;

/// <summary>
/// No conformidad generada a partir de un <see cref="AuditResult"/> en No Cumple (HU-16 a HU-19).
/// Encapsula la máquina de estados de cierre, incluyendo el flujo de validación/rechazo del
/// Auditor Líder sobre la evidencia cargada por el Cliente en su portal (HU-18/HU-19).
/// </summary>
public sealed class Finding
{
    public Guid Id { get; private set; }
    public Guid AuditPlanId { get; private set; }
    public Guid AuditResultId { get; private set; }
    public FindingSeverity Severity { get; private set; }
    public string Description { get; private set; } = default!;
    public FindingStatus Status { get; private set; }
    public Guid? ResponsibleUserId { get; private set; }
    public DateOnly? CommitmentDate { get; private set; }
    public string? ClosureEvidenceRef { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    private Finding() { } // EF Core

    private Finding(Guid id, Guid auditPlanId, Guid auditResultId, FindingSeverity severity, string description)
    {
        Id = id;
        AuditPlanId = auditPlanId;
        AuditResultId = auditResultId;
        Severity = severity;
        Description = description;
        Status = FindingStatus.Open;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// HU-16: crea el hallazgo a partir del criterio origen. La transacción que invoca esta fábrica
    /// debe persistir Finding y AuditResult en la misma unidad de trabajo (5.1 SRS) — no puede
    /// existir un hallazgo huérfano sin criterio origen, ni un criterio No Cumple sin hallazgo.
    /// </summary>
    public static Finding RaiseFrom(AuditResult origin, FindingSeverity severity, string description)
    {
        ArgumentNullException.ThrowIfNull(origin);

        if (!origin.RequiresFinding())
            throw new DomainException("Solo puede generarse un hallazgo a partir de un criterio marcado como No Cumple.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("El hallazgo requiere una descripción.");

        return new Finding(Guid.NewGuid(), origin.AuditPlanId, origin.Id, severity, description);
    }

    /// <summary>HU-17: asigna responsable y fecha compromiso; no admite fecha anterior a la del hallazgo.</summary>
    public void AssignResponsible(Guid responsibleUserId, DateOnly commitmentDate)
    {
        EnsureActionable();

        if (responsibleUserId == Guid.Empty)
            throw new DomainException("Se requiere un responsable válido.");

        if (commitmentDate < DateOnly.FromDateTime(CreatedAtUtc))
            throw new DomainException("La fecha compromiso no puede ser anterior a la fecha del hallazgo.");

        ResponsibleUserId = responsibleUserId;
        CommitmentDate = commitmentDate;

        if (Status == FindingStatus.Open)
            Status = FindingStatus.InProgress;
    }

    /// <summary>HU-18: el Cliente carga evidencia de cierre desde su portal; pasa a revisión del Auditor Líder.</summary>
    public void SubmitClosureEvidence(string evidenceRef)
    {
        if (Status != FindingStatus.InProgress)
            throw new DomainException($"No se puede recibir evidencia de cierre en estado {Status}.");

        if (string.IsNullOrWhiteSpace(evidenceRef))
            throw new DomainException("Se requiere una referencia de evidencia de cierre válida.");

        ClosureEvidenceRef = evidenceRef;
        Status = FindingStatus.InReview;
    }

    /// <summary>HU-19: el Auditor Líder valida la evidencia y cierra el hallazgo. Transición irreversible.</summary>
    public void Approve()
    {
        if (Status != FindingStatus.InReview)
            throw new DomainException($"Solo un hallazgo En Revisión puede cerrarse (estado actual: {Status}).");

        Status = FindingStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
    }

    /// <summary>HU-19: el Auditor Líder rechaza la evidencia; el hallazgo requiere corrección del Cliente.</summary>
    public void Reject(string reason)
    {
        if (Status != FindingStatus.InReview)
            throw new DomainException($"Solo un hallazgo En Revisión puede rechazarse (estado actual: {Status}).");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Se requiere un motivo de rechazo.");

        Status = FindingStatus.Rejected;
        RejectionReason = reason;
    }

    /// <summary>Reabre un hallazgo Rechazado para que el Cliente vuelva a someter evidencia de cierre.</summary>
    public void Reopen()
    {
        if (Status != FindingStatus.Rejected)
            throw new DomainException($"Solo un hallazgo Rechazado puede reabrirse (estado actual: {Status}).");

        Status = FindingStatus.InProgress;
        ClosureEvidenceRef = null;
        RejectionReason = null;
    }

    /// <summary>HU-20: soporta el semáforo de vencimiento (Verde/Amarillo/Rojo) en el dashboard.</summary>
    public bool IsOverdue() =>
        Status is FindingStatus.Open or FindingStatus.InProgress &&
        CommitmentDate is { } commitment &&
        commitment < DateOnly.FromDateTime(DateTime.UtcNow);

    private void EnsureActionable()
    {
        if (Status is FindingStatus.Closed)
            throw new DomainException("El hallazgo está Cerrado y no admite modificaciones.");
    }
}
