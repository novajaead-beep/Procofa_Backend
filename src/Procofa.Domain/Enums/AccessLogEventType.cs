namespace Procofa.Domain.Enums;

/// <summary>
/// Tipo de evento de acceso registrado en <c>access_logs</c>.
/// Respaldado por <c>event_type varchar(40)</c> con
/// <c>CHECK (event_type IN ('LOGIN_SUCCESS','LOGIN_FAILURE','LOGOUT',
/// 'PASSWORD_RESET_REQUEST','PASSWORD_RESET_SUCCESS','ACCOUNT_LOCKED'))</c>.
///
/// Nota (baseline V2.1, decisión congelada #8): <c>RefreshToken</c> NO
/// escribe en <c>access_logs</c> — ninguno de estos 6 valores le corresponde;
/// usa structured logging por ahora. Ampliar este CHECK es una migración
/// futura explícita, no parte de esta instrucción.
/// </summary>
public enum AccessLogEventType
{
    LoginSuccess,
    LoginFailure,
    Logout,
    PasswordResetRequest,
    PasswordResetSuccess,
    AccountLocked
}
