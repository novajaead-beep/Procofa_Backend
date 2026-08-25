using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Identity;

/// <summary>
/// Registro de un evento de acceso (login/logout/reset de contraseña).
/// Entidad independiente, infraestructura de seguridad — no pertenece al
/// aggregate <c>User</c>. Tabla física: <c>access_logs</c>, tenant-scoped,
/// RLS+FORCE RLS. Sin <c>updated_at_utc</c> (append-only por convención de
/// uso, aunque a diferencia de <c>audit_logs</c> no tiene trigger que lo
/// fuerce a nivel de BD).
/// </summary>
public sealed class AccessLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? AttemptedEmail { get; private set; }
    public AccessLogEventType EventType { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AccessLog() { }

    public AccessLog(
        Guid id,
        Guid tenantId,
        Guid? userId,
        string? attemptedEmail,
        AccessLogEventType eventType,
        string? ipAddress,
        string? userAgent)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        AttemptedEmail = attemptedEmail;
        EventType = eventType;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
