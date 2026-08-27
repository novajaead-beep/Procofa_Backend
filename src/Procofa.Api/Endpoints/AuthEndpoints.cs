using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Auth;
using Procofa.Application.UseCases.Auth.Login;

namespace Procofa.Api.Endpoints;

/// <summary>
/// Endpoints de Auth (Instrucción 04, alcance estricto: solo login). Sin
/// lógica de negocio aquí — el endpoint solo (a) valida la forma del
/// request, (b) traduce HttpContext -> <see cref="LoginCommand"/>, (c)
/// traduce <see cref="LoginResult"/> -> respuesta HTTP. Toda decisión real
/// (credenciales, lockout, roles) vive en <see cref="LoginCommandHandler"/>.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", LoginAsync);
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult, ValidationProblem>> LoginAsync(
        LoginRequest request,
        LoginCommandHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(validationErrors);
        }

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgentHeader = httpContext.Request.Headers["User-Agent"].ToString();
        var userAgent = string.IsNullOrWhiteSpace(userAgentHeader) ? null : userAgentHeader;

        var command = new LoginCommand(request.Email!, request.Password!, ipAddress, userAgent);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            // Instrucción 04: las 3 causas de fallo (usuario inexistente, password
            // incorrecto, inactivo, bloqueado) SIEMPRE producen la misma respuesta —
            // "no revelar si el email existe" / "respuesta uniforme para credenciales inválidas".
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Credenciales inválidas.",
                detail: "El email o la contraseña son incorrectos, o la cuenta no puede iniciar sesión en este momento.");
        }

        var response = new LoginResponse(
            result.AccessToken!.Value,
            result.AccessToken.ExpiresAtUtc,
            result.RefreshToken!,
            result.RefreshTokenExpiresAtUtc!.Value,
            result.Roles);

        return TypedResults.Ok(response);
    }

    private static Dictionary<string, string[]> Validate(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["El email es obligatorio."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["La contraseña es obligatoria."];
        }

        return errors;
    }
}
