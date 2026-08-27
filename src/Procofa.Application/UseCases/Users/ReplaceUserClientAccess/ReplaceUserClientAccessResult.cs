namespace Procofa.Application.UseCases.Users.ReplaceUserClientAccess;

public enum ReplaceUserClientAccessError
{
    NotFound,

    /// <summary>Sección "ACCESO A CLIENTES": el usuario no tiene rol CLIENTE — 409.</summary>
    UserNotCliente,

    /// <summary>Alguno de los <c>clientIds</c> no existe, o no pertenece al tenant actual.</summary>
    ClientNotFound,
}

/// <summary>Resultado de <see cref="ReplaceUserClientAccessCommandHandler"/> — construido únicamente vía <see cref="Success"/>/<see cref="Failure"/>.</summary>
public sealed class ReplaceUserClientAccessResult
{
    public bool IsSuccess { get; }
    public ReplaceUserClientAccessError? Error { get; }

    private ReplaceUserClientAccessResult(bool isSuccess, ReplaceUserClientAccessError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static ReplaceUserClientAccessResult Success() => new(true, null);

    public static ReplaceUserClientAccessResult Failure(ReplaceUserClientAccessError error) => new(false, error);
}
