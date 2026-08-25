namespace Procofa.Domain.Enums;

/// <summary>
/// Estado de un mensaje en <c>outbox_messages</c>.
/// Respaldado por <c>status varchar(20) DEFAULT 'PENDING'</c> con
/// <c>CHECK (status IN ('PENDING','PROCESSING','PROCESSED','FAILED'))</c>.
/// El índice parcial <c>ix_outbox_pending</c> filtra por
/// <c>status IN ('PENDING','FAILED')</c> — ver
/// <c>OutboxMessageConfiguration</c>.
/// </summary>
public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Processed,
    Failed
}
