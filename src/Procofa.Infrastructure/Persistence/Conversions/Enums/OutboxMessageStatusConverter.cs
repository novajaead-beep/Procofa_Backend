using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="OutboxMessageStatus"/> ↔
/// <c>outbox_messages.status varchar(20)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class OutboxMessageStatusConverter : ValueConverter<OutboxMessageStatus, string>
{
    public OutboxMessageStatusConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(OutboxMessageStatus value) => value switch
    {
        OutboxMessageStatus.Pending => "PENDING",
        OutboxMessageStatus.Processing => "PROCESSING",
        OutboxMessageStatus.Processed => "PROCESSED",
        OutboxMessageStatus.Failed => "FAILED",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(OutboxMessageStatus)} sin mapeo físico explícito."),
    };

    private static OutboxMessageStatus FromDb(string value) => value switch
    {
        "PENDING" => OutboxMessageStatus.Pending,
        "PROCESSING" => OutboxMessageStatus.Processing,
        "PROCESSED" => OutboxMessageStatus.Processed,
        "FAILED" => OutboxMessageStatus.Failed,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(OutboxMessageStatus)}."),
    };
}
