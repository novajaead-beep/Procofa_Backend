namespace Procofa.Application.UseCases.Users.ReplaceUserRoles;

public enum ReplaceUserRolesError
{
    NotFound,

    /// <summary>Sin roles, o con roles fuera del catálogo cerrado (<see cref="UserRoleCodes"/>).</summary>
    ValidationFailed,

    /// <summary>Rol dentro del catálogo permitido pero no sembrado en <c>roles</c>.</summary>
    RoleNotFound,

    /// <summary>Sección "ASIGNAR ROLES": "No permitir que un ADMIN elimine su propio rol ADMIN" — 409.</summary>
    CannotRemoveOwnAdminRole,
}

/// <summary>Resultado de <see cref="ReplaceUserRolesCommandHandler"/> — construido únicamente vía <see cref="Success"/>/<see cref="Failure"/>.</summary>
public sealed class ReplaceUserRolesResult
{
    public bool IsSuccess { get; }
    public ReplaceUserRolesError? Error { get; }
    public string? ErrorDetail { get; }

    private ReplaceUserRolesResult(bool isSuccess, ReplaceUserRolesError? error, string? errorDetail)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
    }

    public static ReplaceUserRolesResult Success() => new(true, null, null);

    public static ReplaceUserRolesResult Failure(ReplaceUserRolesError error, string? errorDetail = null) =>
        new(false, error, errorDetail);
}
