namespace Procofa.Application.UseCases.Users.ChangeUserStatus;

public enum ChangeUserStatusError
{
    NotFound,

    /// <summary>Sección "ACTIVAR / DESACTIVAR": "Un ADMIN no debe poder desactivar su propia cuenta desde este endpoint" — 409.</summary>
    CannotDeactivateSelf,
}

/// <summary>Resultado de <see cref="ChangeUserStatusCommandHandler"/> — construido únicamente vía <see cref="Success"/>/<see cref="Failure"/>.</summary>
public sealed class ChangeUserStatusResult
{
    public bool IsSuccess { get; }
    public ChangeUserStatusError? Error { get; }

    private ChangeUserStatusResult(bool isSuccess, ChangeUserStatusError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ChangeUserStatusResult Success() => new(true, null);

    public static ChangeUserStatusResult Failure(ChangeUserStatusError error) => new(false, error);
}
