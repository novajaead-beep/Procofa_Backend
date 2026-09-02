using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Audits;
using Procofa.Application.UseCases.Audits.CreateAudit;
using Procofa.Application.UseCases.Audits.GetAudit;
using Procofa.Application.UseCases.Audits.ListAudits;
using Procofa.Application.UseCases.Audits.ReplaceAuditChecklists;
using Procofa.Application.UseCases.Audits.ReplaceAuditPrograms;
using Procofa.Application.UseCases.Audits.ReplaceAuditTeam;
using Procofa.Application.UseCases.Audits.UpdateAudit;
using Procofa.Application.UseCases.Users;

namespace Procofa.Api.Endpoints;

/// <summary>
/// Endpoints de <c>/api/audits</c> — planificación de auditorías (Programs/Team/Checklists).
/// Lectura: cualquier rol autenticado (el alcance de CLIENTE se resuelve en Application — ver <see
/// cref="Procofa.Application.UseCases.Clients.ClientAccessScope"/>). Escritura: exclusiva de
/// ADMIN — AUDITOR_LIDER/AUDITOR_APOYO/CONSULTOR quedan en solo-lectura, sin autorización granular
/// por recurso todavía.
/// </summary>
public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/audits").RequireAuthorization();

        group.MapGet("", ListAuditsAsync);
        group.MapGet("/{auditId:guid}", GetAuditAsync);

        group.MapPost("", CreateAuditAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{auditId:guid}", UpdateAuditAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{auditId:guid}/programs", ReplaceAuditProgramsAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{auditId:guid}/team", ReplaceAuditTeamAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{auditId:guid}/checklists", ReplaceAuditChecklistsAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
    }

    private static async Task<Results<Ok<AuditListResponse>, ValidationProblem>> ListAuditsAsync(
        Guid? clientId,
        Guid? companyId,
        string? status,
        Guid? auditTypeId,
        string? executionMode,
        string? search,
        int? page,
        int? pageSize,
        ListAuditsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListAuditsQuery(
                clientId, companyId, status, auditTypeId, executionMode, search, page ?? 1, pageSize ?? 25),
            cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = [result.ErrorDetail ?? "Request inválido."],
            });
        }

        var items = result.Items
            .Select(a => new AuditListItemResponse(
                a.Id, a.Folio, a.ClientId, a.AuditedCompanyId, a.CompanySiteId, a.AuditTypeId, a.ProfileId,
                a.StatusId, a.Objective, a.ScheduledDate, a.StartedAtUtc, a.ExecutionMode, a.CreatedAtUtc))
            .ToArray();

        return TypedResults.Ok(new AuditListResponse(items, result.Page, result.PageSize, result.Total));
    }

    private static async Task<Results<Ok<AuditDetailResponse>, NotFound>> GetAuditAsync(
        Guid auditId, GetAuditQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAuditQuery(auditId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        var team = result.Team
            .Select(m => new AuditTeamMemberResponse(m.UserId, m.Role, m.AssignedByUserId, m.AssignedAtUtc))
            .ToArray();

        var checklists = result.Checklists
            .Select(c => new AuditChecklistItemResponse(
                c.AuditChecklistId, c.ChecklistId, c.ChecklistVersionId, c.VersionNumber, c.ChecklistName))
            .ToArray();

        return TypedResults.Ok(new AuditDetailResponse(
            result.Id, result.Folio, result.ClientId, result.AuditedCompanyId, result.CompanySiteId,
            result.AuditTypeId, result.ProfileId, result.StatusId, result.Objective, result.Scope,
            result.Methodology, result.ScheduledDate, result.StartedAtUtc, result.FinishedAtUtc,
            result.ClosedAtUtc, result.ExecutionMode, result.IsEditable, result.ProgramCodes, team, checklists,
            result.CreatedAtUtc, result.UpdatedAtUtc));
    }

    private static async Task<Results<Created<CreateAuditResponse>, ValidationProblem, ProblemHttpResult>> CreateAuditAsync(
        CreateAuditRequest request, CreateAuditCommandHandler handler, CancellationToken cancellationToken)
    {
        var command = new CreateAuditCommand(
            request.ClientId, request.AuditedCompanyId, request.CompanySiteId, request.AuditTypeId,
            request.ProfileId, request.ProgramCodes, request.Objective, request.Scope, request.Methodology,
            request.ScheduledDate, request.ExecutionMode);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                CreateAuditError.ClientNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Cliente no encontrado.",
                    detail: result.ErrorDetail),
                CreateAuditError.AuditedCompanyNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Empresa auditada no encontrada.",
                    detail: result.ErrorDetail),
                CreateAuditError.CompanySiteNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Sede no encontrada.",
                    detail: result.ErrorDetail),
                CreateAuditError.AuditTypeNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Tipo de auditoría no encontrado.",
                    detail: result.ErrorDetail),
                CreateAuditError.ProfileNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Perfil no encontrado.",
                    detail: result.ErrorDetail),
                CreateAuditError.ProgramNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Programa no encontrado.",
                    detail: result.ErrorDetail),
                CreateAuditError.StatusNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Catálogo de estados incompleto.",
                    detail: result.ErrorDetail),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        var response = new CreateAuditResponse(result.AuditId!.Value, result.Folio!);
        return TypedResults.Created($"/api/audits/{response.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> UpdateAuditAsync(
        Guid auditId, UpdateAuditRequest request, UpdateAuditCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAuditCommand(
            auditId, request.AuditedCompanyId, request.CompanySiteId, request.AuditTypeId, request.ProfileId,
            request.Objective, request.Scope, request.Methodology, request.ScheduledDate, request.ExecutionMode);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                UpdateAuditError.NotFound => TypedResults.NotFound(),
                UpdateAuditError.NotEditable => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "La auditoría ya inició ejecución: no admite cambios de planificación."),
                UpdateAuditError.AuditedCompanyNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Empresa auditada no encontrada.",
                    detail: result.ErrorDetail),
                UpdateAuditError.CompanySiteNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Sede no encontrada.",
                    detail: result.ErrorDetail),
                UpdateAuditError.AuditTypeNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Tipo de auditoría no encontrado.",
                    detail: result.ErrorDetail),
                UpdateAuditError.ProfileNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Perfil no encontrado.",
                    detail: result.ErrorDetail),
                UpdateAuditError.ChecklistIncompatible => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Checklist ya asignado es incompatible con el nuevo profile/audit_type.",
                    detail: result.ErrorDetail),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ReplaceAuditProgramsAsync(
        Guid auditId, ReplaceAuditProgramsRequest request, ReplaceAuditProgramsCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReplaceAuditProgramsCommand(auditId, request.ProgramCodes), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                ReplaceAuditProgramsError.NotFound => TypedResults.NotFound(),
                ReplaceAuditProgramsError.NotEditable => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "La auditoría ya inició ejecución: no admite cambios de planificación."),
                ReplaceAuditProgramsError.ChecklistOrphaned => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Un checklist ya asignado depende de un programa fuera del nuevo conjunto.",
                    detail: result.ErrorDetail),
                _ => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Programa no encontrado.",
                    detail: result.ErrorDetail),
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> ReplaceAuditTeamAsync(
        Guid auditId, ReplaceAuditTeamRequest request, ReplaceAuditTeamCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var members = (request.Members ?? [])
            .Select(m => new ReplaceAuditTeamMemberInput(m.UserId, m.Role))
            .ToArray();

        var result = await handler.HandleAsync(new ReplaceAuditTeamCommand(auditId, members), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                ReplaceAuditTeamError.NotFound => TypedResults.NotFound(),
                ReplaceAuditTeamError.NotEditable => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "La auditoría ya inició ejecución: no admite cambios de planificación."),
                ReplaceAuditTeamError.UserNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Usuario no encontrado.",
                    detail: result.ErrorDetail),
                ReplaceAuditTeamError.DuplicateUser => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Usuario duplicado en el equipo.",
                    detail: result.ErrorDetail),
                ReplaceAuditTeamError.MultipleLeads => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Más de un LEAD en el equipo.",
                    detail: result.ErrorDetail),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ReplaceAuditChecklistsAsync(
        Guid auditId, ReplaceAuditChecklistsRequest request, ReplaceAuditChecklistsCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ReplaceAuditChecklistsCommand(auditId, request.ChecklistIds), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                ReplaceAuditChecklistsError.NotFound => TypedResults.NotFound(),
                ReplaceAuditChecklistsError.NotEditable => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "La auditoría ya inició ejecución: no admite cambios de planificación."),
                ReplaceAuditChecklistsError.ChecklistNotFound => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Checklist no encontrado.",
                    detail: result.ErrorDetail),
                ReplaceAuditChecklistsError.IncompatibleChecklist => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Checklist incompatible con la auditoría.",
                    detail: result.ErrorDetail),
                _ => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "El checklist no tiene versión publicada.",
                    detail: result.ErrorDetail),
            };
        }

        return TypedResults.NoContent();
    }
}
