namespace Procofa.Domain.Enums;

/// <summary>
/// Modalidad de ejecución de una <c>Audit</c>.
/// Respaldado en BD por <c>audits.execution_mode varchar(20)</c> con
/// <c>CONSTRAINT ck_audits_execution_mode CHECK (execution_mode IN
/// ('ONSITE','REMOTE','HYBRID'))</c> — sin catálogo propio, sin tabla FK.
///
/// La obligatoriedad condicional de <c>company_site_id</c> cuando el modo es
/// <see cref="Onsite"/> NO existe como CHECK ni trigger en la BD (verificado
/// contra el dump: <c>company_site_id</c> es nullable a nivel de columna,
/// sin CHECK cruzado). Es intencional: la regla vive en Domain/Application,
/// no en PostgreSQL (Instrucción 03 / baseline V2.1, sección D).
/// </summary>
public enum ExecutionMode
{
    Onsite,
    Remote,
    Hybrid
}
