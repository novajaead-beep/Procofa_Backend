using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Clients;
using Procofa.Application.UseCases.Clients.ChangeClientStatus;
using Procofa.Application.UseCases.Clients.CreateClient;
using Procofa.Application.UseCases.Clients.GetClient;
using Procofa.Application.UseCases.Clients.ListClients;
using Procofa.Application.UseCases.Clients.UpdateClient;
using Procofa.Application.UseCases.Users;

namespace Procofa.Api.Endpoints;

/// <summary>
/// Endpoints de <c>/api/clients</c>. Lectura: cualquier rol autenticado (el alcance de CLIENTE se
/// resuelve en Application — ver <see
/// cref="Procofa.Application.UseCases.Clients.ClientAccessScope"/>). Escritura: exclusiva de ADMIN.
/// </summary>
public static class ClientEndpoints
{
    public static void MapClientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients").RequireAuthorization();

        group.MapGet("", ListClientsAsync);
        group.MapGet("/{clientId:guid}", GetClientAsync);

        group.MapPost("", CreateClientAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{clientId:guid}", UpdateClientAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPatch("/{clientId:guid}/status", ChangeClientStatusAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
    }

    private static async Task<Ok<ClientListResponse>> ListClientsAsync(
        string? search,
        bool? isActive,
        string? program,
        int? page,
        int? pageSize,
        ListClientsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListClientsQuery(search, isActive, program, page ?? 1, pageSize ?? 25), cancellationToken);

        var items = result.Items
            .Select(c => new ClientListItemResponse(
                c.Id, c.LegalName, c.TradeName, c.TaxId, c.IsActive, c.Programs, c.AuditedCompanyCount,
                c.CreatedAtUtc))
            .ToArray();

        return TypedResults.Ok(new ClientListResponse(items, result.Page, result.PageSize, result.Total));
    }

    private static async Task<Results<Ok<ClientDetailResponse>, NotFound>> GetClientAsync(
        Guid clientId, GetClientQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetClientQuery(clientId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new ClientDetailResponse(
            result.Id, result.LegalName, result.TradeName, result.TaxId, result.Industry, result.CompanyType,
            result.Notes, result.IsActive, result.Programs, result.AuditedCompanyCount, result.CreatedAtUtc,
            result.UpdatedAtUtc));
    }

    private static async Task<Results<Created<CreateClientResponse>, ValidationProblem, ProblemHttpResult>> CreateClientAsync(
        CreateClientRequest request, CreateClientCommandHandler handler, CancellationToken cancellationToken)
    {
        var command = new CreateClientCommand(
            request.LegalName, request.TradeName, request.TaxId, request.Industry, request.CompanyType,
            request.Notes, request.Programs);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                CreateClientError.TaxIdAlreadyExists => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "El tax_id ya está en uso.",
                    detail: result.ErrorDetail),
                CreateClientError.ProgramNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Programa no encontrado.",
                    detail: result.ErrorDetail),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        var response = new CreateClientResponse(result.ClientId!.Value);
        return TypedResults.Created($"/api/clients/{response.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> UpdateClientAsync(
        Guid clientId, UpdateClientRequest request, UpdateClientCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateClientCommand(
            clientId, request.LegalName, request.TradeName, request.TaxId, request.Industry, request.CompanyType,
            request.Notes, request.Programs);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                UpdateClientError.NotFound => TypedResults.NotFound(),
                UpdateClientError.TaxIdAlreadyExists => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "El tax_id ya está en uso.",
                    detail: result.ErrorDetail),
                UpdateClientError.ProgramNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Programa no encontrado.",
                    detail: result.ErrorDetail),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> ChangeClientStatusAsync(
        Guid clientId, ChangeStatusRequest request, ChangeClientStatusCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ChangeClientStatusCommand(clientId, request.IsActive), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
