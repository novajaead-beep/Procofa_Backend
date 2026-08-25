namespace Procofa.Domain.Enums;

/// <summary>
/// Origen de una <c>Observation</c> sobre un <c>AuditCriterion</c>.
/// Respaldado por <c>observations.observation_type varchar(30) DEFAULT 'AUDITOR'</c>
/// con <c>CHECK (observation_type IN ('AUDITOR','CLIENTE','INTERNA'))</c>.
/// </summary>
public enum ObservationType
{
    Auditor,
    Cliente,
    Interna
}
