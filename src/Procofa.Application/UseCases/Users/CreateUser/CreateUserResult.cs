namespace Procofa.Application.UseCases.Users.CreateUser;

public enum CreateUserError
{
    /// <summary>Forma del request inválida (campos obligatorios, mínimo un rol, rol fuera del catálogo permitido, clientIds sin rol CLIENTE).</summary>
    ValidationFailed,

    /// <summary>Ya existe un usuario con ese email (normalizado) dentro del tenant.</summary>
    EmailAlreadyExists,

    /// <summary>Alguno de los <c>roles</c> no existe en el catálogo real (<c>roles</c>) — puede pasar el filtro de <see cref="UserRoleCodes"/> y aun así no estar sembrado.</summary>
    RoleNotFound,

    /// <summary>Alguno de los <c>clientIds</c> no existe, o no pertenece al tenant actual.</summary>
    ClientNotFound,
}

/// <summary>Resultado de <see cref="CreateUserCommandHandler"/> — construido únicamente vía <see cref="Success"/>/<see cref="Failure"/>. Nunca incluye la contraseña temporal.</summary>
public sealed class CreateUserResult
{
    public bool IsSuccess { get; }
    public CreateUserError? Error { get; }
    public string? ErrorDetail { get; }
    public Guid? UserId { get; }

    private CreateUserResult(bool isSuccess, CreateUserError? error, string? errorDetail, Guid? userId)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
        UserId = userId;
    }

    public static CreateUserResult Success(Guid userId) => new(true, null, null, userId);

    public static CreateUserResult Failure(CreateUserError error, string errorDetail) =>
        new(false, error, errorDetail, null);
}
