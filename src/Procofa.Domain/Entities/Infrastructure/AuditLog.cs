namespace Procofa.Domain.Entities.Infrastructure;

/// <summary>
/// Entrada de bitácora de auditoría del sistema — append-only por partida
/// doble: el trigger <c>trg_audit_logs_no_update</c>/<c>trg_audit_logs_no_delete</c>
/// (<c>prevent_audit_log_mutation()</c>) rechaza incondicionalmente UPDATE y
/// DELETE, Y el rol <c>procofa_app</c> ni siquiera tiene el privilegio GRANT
/// de UPDATE/DELETE sobre esta tabla (doble enforcement, baseline V2.1
/// sección D). Entidad independiente con <c>DbSet</c> propio.
/// Tabla física: <c>audit_logs</c>, tenant-scoped, RLS+FORCE RLS.
///
/// <see cref="OldValues"/>/<see cref="NewValues"/> mapean <c>jsonb</c> como
/// <c>string</c> — mismo criterio que el resto de columnas jsonb del
/// dominio.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? RoleCode { get; private set; }
    public Guid? AuditId { get; private set; }
    public string EntityName { get; private set; } = null!;
    public Guid? EntityId { get; private set; }
    public string Action { get; private set; } = null!;
    public string? OldValues { get; private set; }
    public string? NewValues { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AuditLog() { }

    public AuditLog(
        Guid id,
        Guid tenantId,
        Guid? userId,
        string? roleCode,
        Guid? auditId,
        string entityName,
        Guid? entityId,
        string action,
        string? oldValues,
        string? newValues,
        string? ipAddress,
        string? userAgent)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        RoleCode = roleCode;
        AuditId = auditId;
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        OldValues = oldValues;
        NewValues = newValues;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
