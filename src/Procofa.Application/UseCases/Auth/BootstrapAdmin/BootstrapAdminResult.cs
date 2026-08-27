namespace Procofa.Application.UseCases.Auth.BootstrapAdmin;

/// <summary>Desenlace del bootstrap — ver <see cref="BootstrapAdminResult"/>.</summary>
public enum BootstrapAdminOutcome
{
    /// <summary>Se creó el primer ADMIN.</summary>
    Created,

    /// <summary>Ya existía un usuario con rol ADMIN en el tenant — no-op idempotente, no es un error.</summary>
    AlreadyExists,

    /// <summary>Datos de entrada inválidos (email/password/nombre faltante, password demasiado corto) — falla de forma segura, nunca crea nada.</summary>
    ValidationFailed,
}

/// <summary>
/// Resultado de <see cref="BootstrapAdminCommandHandler"/>. Construido
/// únicamente vía los factory methods — nunca con constructor público.
/// </summary>
public sealed class BootstrapAdminResult
{
    public BootstrapAdminOutcome Outcome { get; }
    public Guid? UserId { get; }
    public string? ValidationError { get; }

    private BootstrapAdminResult(BootstrapAdminOutcome outcome, Guid? userId, string? validationError)
    {
        Outcome = outcome;
        UserId = userId;
        ValidationError = validationError;
    }

    public static BootstrapAdminResult Created(Guid userId) =>
        new(BootstrapAdminOutcome.Created, userId, null);

    public static BootstrapAdminResult AlreadyExists() =>
        new(BootstrapAdminOutcome.AlreadyExists, null, null);

    public static BootstrapAdminResult Failed(string validationError) =>
        new(BootstrapAdminOutcome.ValidationFailed, null, validationError);
}
