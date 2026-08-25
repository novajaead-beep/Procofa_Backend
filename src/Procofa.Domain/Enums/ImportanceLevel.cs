namespace Procofa.Domain.Enums;

/// <summary>
/// Nivel de importancia opcional de un <c>Criterion</c> de checklist.
/// Respaldado por <c>criteria.importance_level varchar(20)</c> (nullable) con
/// <c>CHECK (importance_level IS NULL OR importance_level IN
/// ('ALTA','MEDIA','BAJA'))</c>.
/// </summary>
public enum ImportanceLevel
{
    Alta,
    Media,
    Baja
}
