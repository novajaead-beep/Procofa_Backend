using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Criteria;
using Procofa.Application.UseCases.Criteria.CreateCriterion;
using Procofa.Application.UseCases.Criteria.DeleteCriterion;
using Procofa.Application.UseCases.Criteria.GetCriterion;
using Procofa.Application.UseCases.Criteria.ListCriteria;
using Procofa.Application.UseCases.Criteria.UpdateCriterion;
using Procofa.Application.UseCases.Users;
using Procofa.Domain.Enums;

namespace Procofa.Api.Endpoints;

public static class CriterionEndpoints
{
    private static readonly string[] ReadRoles =
    [
        UserRoleCodes.Admin,
        UserRoleCodes.AuditorLider,
        UserRoleCodes.AuditorApoyo,
        UserRoleCodes.Consultor,
    ];

    public static void MapCriterionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(
                "/api/checklists/{checklistId:guid}/versions/{versionId:guid}/sections/{sectionId:guid}/criteria")
            .RequireAuthorization(policy => policy.RequireRole(ReadRoles));

        group.MapGet("", ListCriteriaAsync);
        group.MapGet("/{criterionId:guid}", GetCriterionAsync);

        group.MapPost("", CreateCriterionAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{criterionId:guid}", UpdateCriterionAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapDelete("/{criterionId:guid}", DeleteCriterionAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
    }

    private static async Task<Results<Ok<CriterionListResponse>, NotFound>> ListCriteriaAsync(
        Guid checklistId, Guid versionId, Guid sectionId, ListCriteriaQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListCriteriaQuery(checklistId, versionId, sectionId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        var items = result.Items
            .Select(c => new CriterionListItemResponse(c.Id, c.Code, c.AuditQuestion, c.IsMandatory, c.SortOrder))
            .ToArray();

        return TypedResults.Ok(new CriterionListResponse(items));
    }

    private static async Task<Results<Ok<CriterionDetailResponse>, NotFound>> GetCriterionAsync(
        Guid checklistId, Guid versionId, Guid sectionId, Guid criterionId, GetCriterionQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetCriterionQuery(checklistId, versionId, sectionId, criterionId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new CriterionDetailResponse(
            result.Id, result.Code, result.AuditQuestion, result.AuditorInterpretation, result.ExpectedEvidence,
            result.ExpectedEvidenceType, result.ImportanceLevel?.ToString().ToUpperInvariant(),
            result.NormativeReference, result.EvaluationRecommendation, result.IsMandatory, result.SortOrder));
    }

    private static async Task<Results<Created<CreateCriterionResponse>, NotFound, ValidationProblem, ProblemHttpResult>> CreateCriterionAsync(
        Guid checklistId, Guid versionId, Guid sectionId, CreateCriterionRequest request,
        CreateCriterionCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!TryParseImportanceLevel(request.ImportanceLevel, out var importanceLevel))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["importanceLevel"] = ["Debe ser ALTA, MEDIA o BAJA."],
            });
        }

        var command = new CreateCriterionCommand(
            checklistId, versionId, sectionId, request.Code, request.AuditQuestion, request.AuditorInterpretation,
            request.ExpectedEvidence, request.ExpectedEvidenceType, importanceLevel, request.NormativeReference,
            request.EvaluationRecommendation, request.IsMandatory, request.SortOrder);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                CreateCriterionError.SectionNotFound => TypedResults.NotFound(),
                CreateCriterionError.VersionPublished => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión ya está publicada."),
                CreateCriterionError.CodeAlreadyExists => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Ya existe un criterio con ese code en la sección."),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["code y auditQuestion son obligatorios."],
                }),
            };
        }

        var response = new CreateCriterionResponse(result.CriterionId!.Value);
        return TypedResults.Created(
            $"/api/checklists/{checklistId}/versions/{versionId}/sections/{sectionId}/criteria/{response.Id}",
            response);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> UpdateCriterionAsync(
        Guid checklistId, Guid versionId, Guid sectionId, Guid criterionId, UpdateCriterionRequest request,
        UpdateCriterionCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!TryParseImportanceLevel(request.ImportanceLevel, out var importanceLevel))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["importanceLevel"] = ["Debe ser ALTA, MEDIA o BAJA."],
            });
        }

        var command = new UpdateCriterionCommand(
            checklistId, versionId, sectionId, criterionId, request.Code, request.AuditQuestion,
            request.AuditorInterpretation, request.ExpectedEvidence, request.ExpectedEvidenceType, importanceLevel,
            request.NormativeReference, request.EvaluationRecommendation, request.IsMandatory, request.SortOrder);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                UpdateCriterionError.NotFound => TypedResults.NotFound(),
                UpdateCriterionError.VersionPublished => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión ya está publicada."),
                UpdateCriterionError.CodeAlreadyExists => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "Ya existe un criterio con ese code en la sección."),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = ["code y auditQuestion son obligatorios."],
                }),
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteCriterionAsync(
        Guid checklistId, Guid versionId, Guid sectionId, Guid criterionId, DeleteCriterionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new DeleteCriterionCommand(checklistId, versionId, sectionId, criterionId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                DeleteCriterionError.NotFound => TypedResults.NotFound(),
                DeleteCriterionError.VersionPublished => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict, title: "La versión ya está publicada."),
                _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict),
            };
        }

        return TypedResults.NoContent();
    }

    private static bool TryParseImportanceLevel(string? raw, out ImportanceLevel? importanceLevel)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            importanceLevel = null;
            return true;
        }

        if (Enum.TryParse<ImportanceLevel>(raw, ignoreCase: true, out var parsed))
        {
            importanceLevel = parsed;
            return true;
        }

        importanceLevel = null;
        return false;
    }
}
