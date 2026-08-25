using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Infrastructure;

/// <summary>
/// Notificación entregada a un usuario. Entidad independiente con
/// <c>DbSet</c> propio. Tabla física: <c>notifications</c>, tenant-scoped,
/// RLS+FORCE RLS, <c>ON DELETE CASCADE</c> desde <c>users</c>.
/// </summary>
public sealed class Notification
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public NotificationChannel Channel { get; private set; } = NotificationChannel.Internal;
    public string NotificationType { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public string? RelatedEntity { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Notification() { }

    public Notification(
        Guid id,
        Guid tenantId,
        Guid userId,
        NotificationChannel channel,
        string notificationType,
        string title,
        string message,
        string? relatedEntity,
        Guid? relatedEntityId)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        Channel = channel;
        NotificationType = notificationType;
        Title = title;
        Message = message;
        RelatedEntity = relatedEntity;
        RelatedEntityId = relatedEntityId;
        IsRead = false;
    }
}
