using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Infrastructure;

/// <summary>
/// Mensaje transaccional de Outbox — insertado en el mismo
/// <c>SaveChanges</c>/transacción que la operación de dominio que lo generó
/// (vía el mismo <c>ITenantUnitOfWork</c>). Entidad independiente con
/// <c>DbSet</c> propio. Tabla física: <c>outbox_messages</c>, tenant-scoped,
/// RLS+FORCE RLS.
///
/// Índice parcial <c>ix_outbox_pending WHERE status IN ('PENDING','FAILED')</c>
/// — usado por el <c>BackgroundService</c> de despacho (futuro, no esta
/// instrucción).
/// <see cref="Payload"/> mapea <c>jsonb NOT NULL</c> como <c>string</c>.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string? AggregateType { get; private set; }
    public Guid? AggregateId { get; private set; }
    public string Payload { get; private set; } = null!;
    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;

    /// <summary>CHECK &gt;= 0.</summary>
    public int Attempts { get; private set; }

    public DateTime AvailableAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(Guid id, Guid tenantId, string eventType, string? aggregateType, Guid? aggregateId, string payload)
    {
        Id = id;
        TenantId = tenantId;
        EventType = eventType;
        AggregateType = aggregateType;
        AggregateId = aggregateId;
        Payload = payload;
        Status = OutboxMessageStatus.Pending;
        Attempts = 0;
    }
}
