using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.ChecklistVersions;
using Procofa.Application.UseCases.ChecklistVersions.CreateChecklistVersion;
using Procofa.Application.UseCases.ChecklistVersions.GetChecklistVersion;
using Procofa.Application.UseCases.ChecklistVersions.ListChecklistVersions;
using Procofa.Application.UseCases.ChecklistVersions.PublishChecklistVersion;
using Procofa.Application.UseCases.ChecklistVersions.UpdateChecklistVersion;
using Procofa.Application.UseCases.Users;

namespace Procofa.Api.Endpoints;

public static class ChecklistVersionEndpoints
{
    private static readonly string[] ReadRoles =
    [
        UserRoleCodes.Admin,
        UserRoleCodes.AuditorLider,
        UserRoleCodes.AuditorApoyo,
        UserRoleCodes.Consultor,
    ];

    public static void MapChecklistVersionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/checklists/{checklistId:guid}/versions")
            .RequireAuthorization(policy => policy.RequireRole(ReadRoles));

        group.MapGet("", ListChecklistVersionsAsync);
        group.MapGet("/{versionId:guid}", GetChecklistVersionAsync);

        group.MapPost("", CreateChecklistVersionAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{versionId:guid}", UpdateChecklistVersionAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPost("/{versionId:guid}/publish", PublishChecklistVersionAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
    }

    private static async Task<Results<Ok<ChecklistVersionListResponse>, NotFound>> ListChecklistVersionsAsync(
        Guid checklistId, ListChecklistVersionsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListChecklistVersionsQuery(checklistId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        var items = result.Items
            .Select(v => new ChecklistVersionListItemResponse(v.Id, v.VersionNumber, v.Status, v.PublishedAtUtc, v.CreatedAtUtc))
            .ToArray();

        return TypedResults.Ok(new ChecklistVersionListResponse(items));
    }

    private static async Task<Results<Ok<ChecklistVersionDetailResponse>, NotFound>> GetChecklistVersionAsync(
        Guid checklistId, Guid versionId, GetChecklistVersionQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetChecklistVersionQuery(checklistId, versionId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new ChecklistVersionDetailResponse(
            result.Id, result.VersionNumber, result.Status, result.ChangeNotes, result.PublishedAtUtc,
            result.CreatedAtUtc, result.UpdatedAtUtc));
    }

    private static async Task<Results<Created<CreateChecklistVersionResponse>, NotFound>> CreateChecklistVersionAsync(
        Guid checklistId, CreateChecklistVersionRequest request, CreateChecklistVersionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateChecklistVersionCommand(checklistId, request.ChangeNotes), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        var response = new CreateChecklistVersionResponse(result.VersionId!.Value, result.VersionNumber!.Value);
        return TypedResults.Created($"/api/checklists/{checklistId}/versions/{response.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> UpdateChecklistVersionAsync(
        Guid checklistId, Guid versionId, UpdateChecklistVersionRequest request,
        UpdateChecklistVersionCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new UpdateChecklistVersionCommand(checklistId, versionId, request.ChangeNotes), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                UpdateChecklistVersionError.NotFound => TypedResults.NotFound(),
                _ => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión ya no admite modificaciones."),
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> PublishChecklistVersionAsync(
        Guid checklistId, Guid versionId, PublishChecklistVersionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PublishChecklistVersionCommand(checklistId, versionId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                PublishChecklistVersionError.NotFound => TypedResults.NotFound(),
                PublishChecklistVersionError.AlreadyPublished => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión ya está publicada."),
                PublishChecklistVersionError.NoSections => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión no tiene secciones."),
                PublishChecklistVersionError.NoCriteria => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión no tiene criterios."),
                _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict),
            };
        }

        return TypedResults.NoContent();
    }
}
