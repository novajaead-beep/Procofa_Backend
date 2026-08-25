using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Infrastructure;

/// <summary>
/// Registro de una operación idempotente (autosave/reintentos de red).
/// Entidad independiente con <c>DbSet</c> propio. Tabla física:
/// <c>idempotency_operations</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>users</c>.
/// Invariante <c>(tenant_id, operation_id)</c> único.
///
/// <see cref="ResponsePayload"/> mapea <c>jsonb</c> como <c>string</c> —
/// Domain agnóstico de la librería de serialización (ver
/// <see cref="Reports.ReportTemplateVersion.ConfigurationJson"/>).
/// </summary>
public sealed class IdempotencyOperation
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OperationId { get; private set; }
    public string OperationType { get; private set; } = null!;
    public string? RequestHash { get; private set; }
    public string? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public IdempotencyOperationStatus Status { get; private set; } = IdempotencyOperationStatus.InProgress;
    public int? ResponseStatusCode { get; private set; }
    public string? ResponsePayload { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private IdempotencyOperation() { }

    public IdempotencyOperation(
        Guid id,
        Guid tenantId,
        Guid userId,
        Guid operationId,
        string operationType,
        string? requestHash,
        string? resourceType,
        Guid? resourceId)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        OperationId = operationId;
        OperationType = operationType;
        RequestHash = requestHash;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Status = IdempotencyOperationStatus.InProgress;
    }
}
