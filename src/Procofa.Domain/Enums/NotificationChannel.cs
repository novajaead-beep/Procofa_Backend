namespace Procofa.Domain.Enums;

/// <summary>
/// Canal de entrega de una <c>Notification</c>.
/// Respaldado por <c>notifications.channel varchar(20) DEFAULT 'INTERNAL'</c>
/// con <c>CHECK (channel IN ('INTERNAL','EMAIL'))</c>.
/// </summary>
public enum NotificationChannel
{
    Internal,
    Email
}
