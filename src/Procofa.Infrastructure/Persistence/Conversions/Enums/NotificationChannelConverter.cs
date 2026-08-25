using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="NotificationChannel"/> ↔
/// <c>notifications.channel varchar(20)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class NotificationChannelConverter : ValueConverter<NotificationChannel, string>
{
    public NotificationChannelConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(NotificationChannel value) => value switch
    {
        NotificationChannel.Internal => "INTERNAL",
        NotificationChannel.Email => "EMAIL",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(NotificationChannel)} sin mapeo físico explícito."),
    };

    private static NotificationChannel FromDb(string value) => value switch
    {
        "INTERNAL" => NotificationChannel.Internal,
        "EMAIL" => NotificationChannel.Email,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(NotificationChannel)}."),
    };
}
