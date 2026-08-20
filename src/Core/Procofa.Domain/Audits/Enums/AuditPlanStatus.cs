namespace Procofa.Domain.Audits.Enums;

/// <summary>Ciclo de vida del plan de auditoría. Transiciones válidas: Planned → InProgress → Closed | Cancelled.</summary>
public enum AuditPlanStatus
{
    Planned = 1,
    InProgress = 2,
    Closed = 3,
    Cancelled = 4
}
