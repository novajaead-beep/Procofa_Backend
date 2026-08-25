namespace Procofa.Domain.Entities.Catalogs;

/// <summary>
/// Estado del ciclo de vida de una <c>Audit</c> (BORRADOR, PROGRAMADA,
/// EN_PROCESO, REVISION, SEGUIMIENTO, CERRADA*, CANCELADA* — *terminales).
/// Catálogo global sin <c>tenant_id</c>, sin RLS, solo lectura para
/// <c>procofa_app</c>. Tabla física: <c>audit_statuses</c>, 7 filas
/// sembradas.
///
/// El grafo de transiciones válidas NO está definido ni en la BD ni en la
/// documentación (baseline V2.1, hallazgo 🟡 sección C) — se define antes de
/// implementar el módulo de Ejecución (Fase 5), no bloquea esta instrucción.
///
/// El trigger <c>trg_audits_validate_close</c>
/// (<c>validate_audit_before_close()</c>) resuelve este catálogo por
/// <c>code = 'CERRADA'</c> para aplicar sus validaciones de cierre.
/// </summary>
public sealed class AuditStatus
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsTerminal { get; private set; }

    private AuditStatus() { }

    public AuditStatus(Guid id, string code, string name, int sortOrder, bool isTerminal)
    {
        Id = id;
        Code = code;
        Name = name;
        SortOrder = sortOrder;
        IsTerminal = isTerminal;
    }
}
