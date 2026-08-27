using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Users;
using Procofa.Application.UseCases.Users;
using Procofa.Application.UseCases.Users.ChangeUserStatus;
using Procofa.Application.UseCases.Users.CreateUser;
using Procofa.Application.UseCases.Users.GetUser;
using Procofa.Application.UseCases.Users.ListUsers;
using Procofa.Application.UseCases.Users.ReplaceUserClientAccess;
using Procofa.Application.UseCases.Users.ReplaceUserRoles;

namespace Procofa.Api.Endpoints;

/// <summary>
/// Endpoints de gestión de usuarios (Instrucción 05) — todos bajo
/// <c>/api/users</c>, todos exclusivos de ADMIN (<see cref="MapUserEndpoints"/>
/// exige el rol a nivel de grupo, una sola vez). Sin lógica de negocio aquí:
/// cada handler solo (a) valida la FORMA del request, (b) traduce a un
/// comando/query de Application, (c) traduce el resultado a HTTP. 401 lo
/// produce el middleware de autenticación si no hay JWT válido; 403 lo
/// produce el middleware de autorización si el rol no es ADMIN — ninguno de
/// los dos se maneja aquí explícitamente.
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));

        group.MapGet("", ListUsersAsync);
        group.MapGet("/{userId:guid}", GetUserAsync);
        group.MapPost("", CreateUserAsync);
        group.MapPatch("/{userId:guid}/status", ChangeUserStatusAsync);
        group.MapPut("/{userId:guid}/roles", ReplaceUserRolesAsync);
        group.MapPut("/{userId:guid}/client-access", ReplaceUserClientAccessAsync);
    }

    // ---- 3.1 Listar usuarios --------------------------------------------

    private static async Task<Results<Ok<UserListResponse>, ValidationProblem>> ListUsersAsync(
        string? search,
        bool? isActive,
        string? role,
        int? page,
        int? pageSize,
        ListUsersQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new ListUsersQuery(search, isActive, role, page ?? 1, pageSize ?? 25);
        var result = await handler.HandleAsync(query, cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["role"] = [$"'{role}' no es un rol válido. Roles permitidos: {string.Join(", ", UserRoleCodes.All)}."],
            });
        }

        var items = result.Items
            .Select(u => new UserListItemResponse(
                u.Id, u.Email, u.FirstName, u.LastName, u.Phone, u.IsActive, u.MustChangePassword,
                u.Roles, u.CreatedAtUtc))
            .ToArray();

        return TypedResults.Ok(new UserListResponse(items, result.Page, result.PageSize, result.Total));
    }

    // ---- 3.2 Detalle de usuario ------------------------------------------

    private static async Task<Results<Ok<UserDetailResponse>, NotFound>> GetUserAsync(
        Guid userId,
        GetUserQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetUserQuery(userId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        var response = new UserDetailResponse(
            result.Id, result.Email, result.FirstName, result.LastName, result.Phone, result.IsActive,
            result.MustChangePassword, result.FailedLoginAttempts, result.LockedUntilUtc, result.LastLoginAtUtc,
            result.CreatedAtUtc, result.UpdatedAtUtc, result.Roles,
            result.ClientAccess.Select(a => new UserClientAccessResponse(a.ClientId)).ToArray());

        return TypedResults.Ok(response);
    }

    // ---- 4. Crear usuario -------------------------------------------------

    private static async Task<Results<Created<CreateUserResponse>, ValidationProblem, ProblemHttpResult>> CreateUserAsync(
        CreateUserRequest request,
        CreateUserCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(
            request.Email, request.FirstName, request.LastName, request.Phone,
            request.TemporaryPassword, request.Roles, request.ClientIds);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                CreateUserError.EmailAlreadyExists => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "El email ya está en uso.",
                    detail: result.ErrorDetail),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        var response = new CreateUserResponse(result.UserId!.Value);
        return TypedResults.Created($"/api/users/{response.Id}", response);
    }

    // ---- 6. Activar / desactivar ------------------------------------------

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ChangeUserStatusAsync(
        Guid userId,
        ChangeUserStatusRequest request,
        ChangeUserStatusCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ChangeUserStatusCommand(userId, request.IsActive), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                ChangeUserStatusError.NotFound => TypedResults.NotFound(),
                ChangeUserStatusError.CannotDeactivateSelf => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "No puede desactivar su propia cuenta.",
                    detail: "Un ADMIN no puede desactivarse a sí mismo desde este endpoint."),
                _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict),
            };
        }

        return TypedResults.NoContent();
    }

    // ---- 7. Asignar roles ---------------------------------------------------

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> ReplaceUserRolesAsync(
        Guid userId,
        ReplaceUserRolesRequest request,
        ReplaceUserRolesCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ReplaceUserRolesCommand(userId, request.Roles), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                ReplaceUserRolesError.NotFound => TypedResults.NotFound(),
                ReplaceUserRolesError.CannotRemoveOwnAdminRole => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "No puede quitarse su propio rol ADMIN.",
                    detail: "Un ADMIN no puede eliminar su propio rol ADMIN mediante este endpoint."),
                ReplaceUserRolesError.ValidationFailed or ReplaceUserRolesError.RoleNotFound =>
                    TypedResults.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["roles"] = [result.ErrorDetail ?? "Roles inválidos."],
                    }),
                _ => TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest),
            };
        }

        return TypedResults.NoContent();
    }

    // ---- 8. Acceso a clientes -----------------------------------------------

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> ReplaceUserClientAccessAsync(
        Guid userId,
        ReplaceUserClientAccessRequest request,
        ReplaceUserClientAccessCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReplaceUserClientAccessCommand(userId, request.ClientIds), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                ReplaceUserClientAccessError.NotFound => TypedResults.NotFound(),
                ReplaceUserClientAccessError.UserNotCliente => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "El usuario no tiene rol CLIENTE.",
                    detail: "Solo se puede administrar acceso a clientes para usuarios con rol CLIENTE."),
                ReplaceUserClientAccessError.ClientNotFound => TypedResults.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        ["clientIds"] = ["Uno o más clientIds no existen o no pertenecen al tenant actual."],
                    }),
                _ => TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest),
            };
        }

        return TypedResults.NoContent();
    }
}
