using Procofa.Domain.Audits.Enums;
using Procofa.Domain.Common;

namespace Procofa.Domain.Audits;

/// <summary>
/// Aggregate root del contexto de Planificación/Ejecución. Encapsula el checklist versionado
/// (snapshot inmutable, HU-03) y las reglas de transición de estado del ciclo PHVA de la auditoría.
/// Los <see cref="AuditResult"/> y <see cref="Finding"/> se tratan como aggregates independientes
/// (referenciados por AuditPlanId) para permitir escritura de alta frecuencia (autosave) sin
/// cargar el aggregate completo — ver IAuditRepository.
/// </summary>
public sealed class AuditPlan
{
    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public AuditProfileType ProfileType { get; private set; }
    public Guid ChecklistMasterVersionId { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public AuditPlanStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    /// <summary>Token de concurrencia optimista mapeado a la columna de sistema `xmin` de PostgreSQL (ver Infrastructure).</summary>
    public uint RowVersion { get; private set; }

    private readonly List<Guid> _teamMemberIds = new();
    public IReadOnlyCollection<Guid> TeamMemberIds => _teamMemberIds.AsReadOnly();

    private readonly List<CriterionSnapshot> _checklist = new();
    public IReadOnlyCollection<CriterionSnapshot> Checklist => _checklist.AsReadOnly();

    private AuditPlan() { } // EF Core

    private AuditPlan(Guid id, Guid clientId, AuditProfileType profileType, Guid checklistMasterVersionId, DateOnly scheduledDate)
    {
        Id = id;
        ClientId = clientId;
        ProfileType = profileType;
        ChecklistMasterVersionId = checklistMasterVersionId;
        ScheduledDate = scheduledDate;
        Status = AuditPlanStatus.Planned;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>HU-02: crea el plan; exige folio de checklist maestro vigente y al menos un auditor asignado.</summary>
    public static AuditPlan Create(
        Guid clientId,
        AuditProfileType profileType,
        Guid checklistMasterVersionId,
        DateOnly scheduledDate,
        IEnumerable<Guid> initialTeamMemberIds)
    {
        if (clientId == Guid.Empty)
            throw new DomainException("El plan de auditoría requiere un cliente válido.");

        if (checklistMasterVersionId == Guid.Empty)
            throw new DomainException("El plan de auditoría requiere una versión de checklist maestro vigente.");

        var team = (initialTeamMemberIds ?? Enumerable.Empty<Guid>()).Distinct().ToList();
        if (team.Count == 0)
            throw new DomainException("No es posible crear un plan de auditoría sin al menos un auditor asignado (HU-02).");

        var plan = new AuditPlan(Guid.NewGuid(), clientId, profileType, checklistMasterVersionId, scheduledDate);
        plan._teamMemberIds.AddRange(team);
        return plan;
    }

    /// <summary>
    /// HU-03: carga automática y única del checklist versionado. Es una copia (snapshot) del maestro,
    /// no una referencia editable — cambios posteriores al maestro no afectan este plan.
    /// </summary>
    public void LoadChecklistSnapshot(IEnumerable<CriterionSnapshot> criteria)
    {
        if (Status != AuditPlanStatus.Planned)
            throw new DomainException("El checklist solo puede cargarse mientras el plan está en estado Planificada.");

        if (_checklist.Count > 0)
            throw new DomainException("El checklist ya fue cargado para este plan; la carga es de una sola vez e inmutable.");

        var snapshot = criteria.ToList();
        if (snapshot.Count == 0)
            throw new DomainException("El checklist maestro del perfil seleccionado no contiene criterios.");

        if (snapshot.Any(c => c.AuditPlanId != Id))
            throw new DomainException("Todos los criterios del snapshot deben pertenecer a este plan de auditoría.");

        _checklist.AddRange(snapshot);
    }

    public void AssignTeamMember(Guid auditorUserId)
    {
        EnsureMutable();
        if (auditorUserId == Guid.Empty)
            throw new DomainException("Id de auditor inválido.");
        if (!_teamMemberIds.Contains(auditorUserId))
            _teamMemberIds.Add(auditorUserId);
    }

    public void RemoveTeamMember(Guid auditorUserId)
    {
        EnsureMutable();
        if (_teamMemberIds.Count <= 1)
            throw new DomainException("No se puede dejar el plan de auditoría sin auditores asignados.");
        _teamMemberIds.Remove(auditorUserId);
    }

    /// <summary>Inicia la ejecución digital (HU-07); requiere checklist previamente cargado.</summary>
    public void Start()
    {
        if (Status != AuditPlanStatus.Planned)
            throw new DomainException($"No es posible iniciar un plan en estado {Status}.");
        if (_checklist.Count == 0)
            throw new DomainException("No se puede iniciar la ejecución sin un checklist cargado.");

        Status = AuditPlanStatus.InProgress;
    }

    /// <summary>HU-10: % de cumplimiento consolidado, calculado sobre resultados persistidos (no en memoria del cliente).</summary>
    public decimal CalculateCompliance(IReadOnlyCollection<AuditResult> results)
    {
        if (_checklist.Count == 0) return 0m;

        var answered = results.Count(r => r.Value != CriterionResultValue.NotAnswered);
        return Math.Round((decimal)answered / _checklist.Count * 100m, 2);
    }

    /// <summary>
    /// Cierra la auditoría. Regla crítica (5.1 SRS): no debe existir un estado "Cerrada" con criterios
    /// obligatorios sin responder — se valida en la misma transacción de cierre.
    /// </summary>
    public void Close(IReadOnlyCollection<AuditResult> results)
    {
        if (Status != AuditPlanStatus.InProgress)
            throw new DomainException($"Solo un plan En Progreso puede cerrarse (estado actual: {Status}).");

        var mandatoryCriterionIds = _checklist.Where(c => c.IsMandatory).Select(c => c.Id).ToHashSet();
        var answeredMandatoryIds = results
            .Where(r => r.Value != CriterionResultValue.NotAnswered)
            .Select(r => r.CriterionSnapshotId)
            .Where(mandatoryCriterionIds.Contains)
            .ToHashSet();

        if (!mandatoryCriterionIds.SetEquals(answeredMandatoryIds))
            throw new DomainException("No se puede cerrar la auditoría: existen criterios obligatorios sin responder.");

        Status = AuditPlanStatus.Closed;
        ClosedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        EnsureMutable();
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Se requiere un motivo de cancelación.");

        Status = AuditPlanStatus.Cancelled;
    }

    private void EnsureMutable()
    {
        if (Status is AuditPlanStatus.Closed or AuditPlanStatus.Cancelled)
            throw new DomainException($"El plan de auditoría está en estado {Status} y no admite modificaciones.");
    }
}
