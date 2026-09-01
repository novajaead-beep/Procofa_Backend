using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.ChecklistSections;
using Procofa.Application.UseCases.ChecklistSections.CreateChecklistSection;
using Procofa.Application.UseCases.ChecklistSections.DeleteChecklistSection;
using Procofa.Application.UseCases.ChecklistSections.ListChecklistSections;
using Procofa.Application.UseCases.ChecklistSections.UpdateChecklistSection;
using Procofa.Application.UseCases.Users;

namespace Procofa.Api.Endpoints;

public static class ChecklistSectionEndpoints
{
    private static readonly string[] ReadRoles =
    [
        UserRoleCodes.Admin,
        UserRoleCodes.AuditorLider,
        UserRoleCodes.AuditorApoyo,
        UserRoleCodes.Consultor,
    ];

    public static void MapChecklistSectionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/checklists/{checklistId:guid}/versions/{versionId:guid}/sections")
            .RequireAuthorization(policy => policy.RequireRole(ReadRoles));

        group.MapGet("", ListChecklistSectionsAsync);

        group.MapPost("", CreateChecklistSectionAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{sectionId:guid}", UpdateChecklistSectionAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapDelete("/{sectionId:guid}", DeleteChecklistSectionAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
    }

    private static async Task<Results<Ok<ChecklistSectionListResponse>, NotFound>> ListChecklistSectionsAsync(
        Guid checklistId, Guid versionId, ListChecklistSectionsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListChecklistSectionsQuery(checklistId, versionId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        var items = result.Items
            .Select(s => new ChecklistSectionListItemResponse(s.Id, s.Code, s.Name, s.Description, s.SortOrder))
            .ToArray();

        return TypedResults.Ok(new ChecklistSectionListResponse(items));
    }

    private static async Task<Results<Created<CreateChecklistSectionResponse>, NotFound, ValidationProblem, ProblemHttpResult>> CreateChecklistSectionAsync(
        Guid checklistId, Guid versionId, CreateChecklistSectionRequest request,
        CreateChecklistSectionCommandHandler handler, CancellationToken cancellationToken)
    {
        var command = new CreateChecklistSectionCommand(
            checklistId, versionId, request.Code, request.Name, request.Description, request.SortOrder);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                CreateChecklistSectionError.VersionNotFound => TypedResults.NotFound(),
                CreateChecklistSectionError.VersionPublished => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión ya está publicada."),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["name es obligatorio."],
                }),
            };
        }

        var response = new CreateChecklistSectionResponse(result.SectionId!.Value);
        return TypedResults.Created(
            $"/api/checklists/{checklistId}/versions/{versionId}/sections/{response.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> UpdateChecklistSectionAsync(
        Guid checklistId, Guid versionId, Guid sectionId, UpdateChecklistSectionRequest request,
        UpdateChecklistSectionCommandHandler handler, CancellationToken cancellationToken)
    {
        var command = new UpdateChecklistSectionCommand(
            checklistId, versionId, sectionId, request.Code, request.Name, request.Description, request.SortOrder);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                UpdateChecklistSectionError.NotFound => TypedResults.NotFound(),
                UpdateChecklistSectionError.VersionPublished => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión ya está publicada."),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["name es obligatorio."],
                }),
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteChecklistSectionAsync(
        Guid checklistId, Guid versionId, Guid sectionId, DeleteChecklistSectionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new DeleteChecklistSectionCommand(checklistId, versionId, sectionId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                DeleteChecklistSectionError.NotFound => TypedResults.NotFound(),
                DeleteChecklistSectionError.VersionPublished => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión ya está publicada."),
                DeleteChecklistSectionError.HasCriteria => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La sección tiene criterios asociados."),
                _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict),
            };
        }

        return TypedResults.NoContent();
    }
}
