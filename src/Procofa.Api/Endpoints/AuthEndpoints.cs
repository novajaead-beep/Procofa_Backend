using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Auth;
using Procofa.Api.Security;
using Procofa.Application.UseCases.Auth.GetCurrentUser;
using Procofa.Application.UseCases.Auth.Login;
using Procofa.Application.UseCases.Auth.Logout;
using Procofa.Application.UseCases.Auth.RefreshSession;

namespace Procofa.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(
        this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous();

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous();

        group.MapPost("/logout", LogoutAsync)
            .AllowAnonymous();

        group.MapGet("/me", GetCurrentUserAsync)
            .RequireAuthorization();
    }

    private static async Task<
        Results<
            Ok<LoginResponse>,
            ProblemHttpResult,
            ValidationProblem>>
        LoginAsync(
            LoginRequest request,
            LoginCommandHandler handler,
            RefreshCookieManager cookieManager,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);

        if (validationErrors.Count > 0)
        {
            return TypedResults.ValidationProblem(
                validationErrors);
        }

        var ipAddress =
            httpContext.Connection
                .RemoteIpAddress?
                .ToString();

        var userAgentHeader =
            httpContext.Request
                .Headers["User-Agent"]
                .ToString();

        var userAgent =
            string.IsNullOrWhiteSpace(userAgentHeader)
                ? null
                : userAgentHeader;

        var command = new LoginCommand(
            request.Email!,
            request.Password!,
            ipAddress,
            userAgent);

        var result =
            await handler.HandleAsync(
                command,
                cancellationToken);

        if (!result.IsSuccess)
        {
            return InvalidCredentials();
        }

        cookieManager.Write(
            httpContext.Response,
            result.RefreshToken!,
            result.RefreshTokenExpiresAtUtc!.Value);

        return TypedResults.Ok(
            new LoginResponse(
                result.AccessToken!.Value,
                result.AccessToken.ExpiresAtUtc,
                result.Roles));
    }

    private static async Task<
        Results<
            Ok<LoginResponse>,
            ProblemHttpResult>>
        RefreshAsync(
            RefreshSessionCommandHandler handler,
            RefreshCookieManager cookieManager,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var rawRefreshToken =
            cookieManager.Read(
                httpContext.Request);

        if (string.IsNullOrWhiteSpace(
                rawRefreshToken))
        {
            cookieManager.Delete(
                httpContext.Response);

            return InvalidRefreshToken();
        }

        var result =
            await handler.HandleAsync(
                new RefreshSessionCommand(
                    rawRefreshToken),
                cancellationToken);

        if (!result.IsSuccess)
        {
            cookieManager.Delete(
                httpContext.Response);

            return InvalidRefreshToken();
        }

        cookieManager.Write(
            httpContext.Response,
            result.RefreshToken!,
            result.RefreshTokenExpiresAtUtc!.Value);

        return TypedResults.Ok(
            new LoginResponse(
                result.AccessToken!.Value,
                result.AccessToken.ExpiresAtUtc,
                result.Roles));
    }

    private static async Task<NoContent>
        LogoutAsync(
            LogoutCommandHandler handler,
            RefreshCookieManager cookieManager,
            HttpContext httpContext,
            CancellationToken cancellationToken)
    {
        var rawRefreshToken =
            cookieManager.Read(
                httpContext.Request);

        var ipAddress =
            httpContext.Connection
                .RemoteIpAddress?
                .ToString();

        var userAgentHeader =
            httpContext.Request
                .Headers["User-Agent"]
                .ToString();

        var userAgent =
            string.IsNullOrWhiteSpace(userAgentHeader)
                ? null
                : userAgentHeader;

        await handler.HandleAsync(
            new LogoutCommand(
                rawRefreshToken,
                ipAddress,
                userAgent),
            cancellationToken);

        cookieManager.Delete(
            httpContext.Response);

        return TypedResults.NoContent();
    }

    private static async Task<
        Results<
            Ok<CurrentUserResponse>,
            ProblemHttpResult>>
        GetCurrentUserAsync(
            GetCurrentUserQueryHandler handler,
            CancellationToken cancellationToken)
    {
        var result =
            await handler.HandleAsync(
                cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.Problem(
                statusCode:
                    StatusCodes.Status401Unauthorized,
                title:
                    "La sesión ya no es válida.");
        }

        return TypedResults.Ok(
            new CurrentUserResponse(
                result.Id,
                result.Email,
                result.FirstName,
                result.LastName,
                result.Phone,
                result.MustChangePassword,
                result.Roles));
    }

    private static ProblemHttpResult
        InvalidCredentials()
    {
        return TypedResults.Problem(
            statusCode:
                StatusCodes.Status401Unauthorized,
            title:
                "Credenciales inválidas.",
            detail:
                "El email o la contraseña son incorrectos, o la cuenta no puede iniciar sesión en este momento.");
    }

    private static ProblemHttpResult
        InvalidRefreshToken()
    {
        return TypedResults.Problem(
            statusCode:
                StatusCodes.Status401Unauthorized,
            title:
                "La sesión no es válida.",
            detail:
                "La sesión expiró o fue revocada.");
    }

    private static Dictionary<string, string[]>
        Validate(LoginRequest request)
    {
        var errors =
            new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(
                request.Email))
        {
            errors["email"] =
                ["El email es obligatorio."];
        }

        if (string.IsNullOrWhiteSpace(
                request.Password))
        {
            errors["password"] =
                ["La contraseña es obligatoria."];
        }

        return errors;
    }
}
