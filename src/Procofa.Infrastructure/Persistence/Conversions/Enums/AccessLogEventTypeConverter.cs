using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="AccessLogEventType"/> ↔
/// <c>access_logs.event_type varchar(40)</c> (Instrucción 03.1, defecto 1):
/// mapeo uno-a-uno escrito a mano, nunca <c>Enum.ToString()</c> ni
/// transformación automática de mayúsculas/PascalCase→snake_case.
/// </summary>
public sealed class AccessLogEventTypeConverter : ValueConverter<AccessLogEventType, string>
{
    public AccessLogEventTypeConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(AccessLogEventType value) => value switch
    {
        AccessLogEventType.LoginSuccess => "LOGIN_SUCCESS",
        AccessLogEventType.LoginFailure => "LOGIN_FAILURE",
        AccessLogEventType.Logout => "LOGOUT",
        AccessLogEventType.PasswordResetRequest => "PASSWORD_RESET_REQUEST",
        AccessLogEventType.PasswordResetSuccess => "PASSWORD_RESET_SUCCESS",
        AccessLogEventType.AccountLocked => "ACCOUNT_LOCKED",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(AccessLogEventType)} sin mapeo físico explícito."),
    };

    private static AccessLogEventType FromDb(string value) => value switch
    {
        "LOGIN_SUCCESS" => AccessLogEventType.LoginSuccess,
        "LOGIN_FAILURE" => AccessLogEventType.LoginFailure,
        "LOGOUT" => AccessLogEventType.Logout,
        "PASSWORD_RESET_REQUEST" => AccessLogEventType.PasswordResetRequest,
        "PASSWORD_RESET_SUCCESS" => AccessLogEventType.PasswordResetSuccess,
        "ACCOUNT_LOCKED" => AccessLogEventType.AccountLocked,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(AccessLogEventType)}."),
    };
}
