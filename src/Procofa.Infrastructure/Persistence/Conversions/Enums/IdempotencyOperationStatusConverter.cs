using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="IdempotencyOperationStatus"/> ↔
/// <c>idempotency_operations.status varchar(20)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class IdempotencyOperationStatusConverter : ValueConverter<IdempotencyOperationStatus, string>
{
    public IdempotencyOperationStatusConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(IdempotencyOperationStatus value) => value switch
    {
        IdempotencyOperationStatus.InProgress => "IN_PROGRESS",
        IdempotencyOperationStatus.Completed => "COMPLETED",
        IdempotencyOperationStatus.Failed => "FAILED",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(IdempotencyOperationStatus)} sin mapeo físico explícito."),
    };

    private static IdempotencyOperationStatus FromDb(string value) => value switch
    {
        "IN_PROGRESS" => IdempotencyOperationStatus.InProgress,
        "COMPLETED" => IdempotencyOperationStatus.Completed,
        "FAILED" => IdempotencyOperationStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(IdempotencyOperationStatus)}."),
    };
}
