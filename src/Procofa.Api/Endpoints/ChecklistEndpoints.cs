using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Checklists;
using Procofa.Application.UseCases.Checklists.ChangeChecklistStatus;
using Procofa.Application.UseCases.Checklists.CreateChecklist;
using Procofa.Application.UseCases.Checklists.GetChecklist;
using Procofa.Application.UseCases.Checklists.ListChecklists;
using Procofa.Application.UseCases.Checklists.ResolveChecklist;
using Procofa.Application.UseCases.Checklists.UpdateChecklist;
using Procofa.Application.UseCases.Users;

namespace Procofa.Api.Endpoints;

/// <summary>Endpoints de <c>/api/checklists</c>. CLIENTE queda fuera del grupo de lectura — este
/// módulo no se expone a ese rol. Escritura exclusiva de ADMIN.</summary>
public static class ChecklistEndpoints
{
    private static readonly string[] ReadRoles =
    [
        UserRoleCodes.Admin,
        UserRoleCodes.AuditorLider,
        UserRoleCodes.AuditorApoyo,
        UserRoleCodes.Consultor,
    ];

    public static void MapChecklistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/checklists").RequireAuthorization(policy => policy.RequireRole(ReadRoles));

        group.MapGet("/resolve", ResolveChecklistAsync);
        group.MapGet("", ListChecklistsAsync);
        group.MapGet("/{checklistId:guid}", GetChecklistAsync);

        group.MapPost("", CreateChecklistAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{checklistId:guid}", UpdateChecklistAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPatch("/{checklistId:guid}/status", ChangeChecklistStatusAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
    }

    private static async Task<Ok<ChecklistListResponse>> ListChecklistsAsync(
        string? search,
        Guid? program,
        Guid? profile,
        Guid? auditType,
        bool? isActive,
        int? page,
        int? pageSize,
        ListChecklistsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListChecklistsQuery(search, program, profile, auditType, isActive, page ?? 1, pageSize ?? 25),
            cancellationToken);

        var items = result.Items
            .Select(c => new ChecklistListItemResponse(
                c.Id, c.ProgramId, c.ProfileId, c.AuditTypeId, c.Name, c.Description, c.IsActive, c.CreatedAtUtc))
            .ToArray();

        return TypedResults.Ok(new ChecklistListResponse(items, result.Page, result.PageSize, result.Total));
    }

    private static async Task<Results<Ok<ChecklistDetailResponse>, NotFound>> GetChecklistAsync(
        Guid checklistId, GetChecklistQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetChecklistQuery(checklistId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new ChecklistDetailResponse(
            result.Id, result.ProgramId, result.ProfileId, result.AuditTypeId, result.Name, result.Description,
            result.IsActive, result.CreatedAtUtc, result.UpdatedAtUtc));
    }

    private static async Task<Results<Created<CreateChecklistResponse>, ValidationProblem, ProblemHttpResult>> CreateChecklistAsync(
        CreateChecklistRequest request, CreateChecklistCommandHandler handler, CancellationToken cancellationToken)
    {
        var command = new CreateChecklistCommand(
            request.ProgramId, request.ProfileId, request.AuditTypeId, request.Name, request.Description);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                CreateChecklistError.ProgramNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest, title: "Program no encontrado."),
                CreateChecklistError.ProfileNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest, title: "Profile no encontrado."),
                CreateChecklistError.AuditTypeNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest, title: "AuditType no encontrado."),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        var response = new CreateChecklistResponse(result.ChecklistId!.Value);
        return TypedResults.Created($"/api/checklists/{response.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> UpdateChecklistAsync(
        Guid checklistId, UpdateChecklistRequest request, UpdateChecklistCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateChecklistCommand(
            checklistId, request.ProgramId, request.ProfileId, request.AuditTypeId, request.Name,
            request.Description);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                UpdateChecklistError.NotFound => TypedResults.NotFound(),
                UpdateChecklistError.ProgramNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest, title: "Program no encontrado."),
                UpdateChecklistError.ProfileNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest, title: "Profile no encontrado."),
                UpdateChecklistError.AuditTypeNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest, title: "AuditType no encontrado."),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> ChangeChecklistStatusAsync(
        Guid checklistId, ChangeChecklistStatusRequest request, ChangeChecklistStatusCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ChangeChecklistStatusCommand(checklistId, request.IsActive), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<ResolveChecklistResponse>, NotFound, ValidationProblem>> ResolveChecklistAsync(
        string? program, string? profile, string? auditType, ResolveChecklistQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ResolveChecklistQuery(program, profile, auditType), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == ResolveChecklistError.ValidationFailed)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                });
            }

            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new ResolveChecklistResponse(
            result.ChecklistId, result.ChecklistName, result.VersionId, result.VersionNumber, result.IsExactMatch));
    }
}
