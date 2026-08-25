namespace Procofa.Domain.Enums;

/// <summary>
/// Estado de una operación idempotente registrada en <c>idempotency_operations</c>.
/// Respaldado por <c>status varchar(20) DEFAULT 'IN_PROGRESS'</c> con
/// <c>CHECK (status IN ('IN_PROGRESS','COMPLETED','FAILED'))</c>.
/// </summary>
public enum IdempotencyOperationStatus
{
    InProgress,
    Completed,
    Failed
}
