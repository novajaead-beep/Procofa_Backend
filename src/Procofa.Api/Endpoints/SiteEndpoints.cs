using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Clients;
using Procofa.Api.Contracts.Sites;
using Procofa.Application.UseCases.Sites.ChangeSiteStatus;
using Procofa.Application.UseCases.Sites.CreateSite;
using Procofa.Application.UseCases.Sites.GetSite;
using Procofa.Application.UseCases.Sites.ListSites;
using Procofa.Application.UseCases.Sites.UpdateSite;
using Procofa.Application.UseCases.Users;

namespace Procofa.Api.Endpoints;

/// <summary>Endpoints de <c>/api/clients/{clientId}/companies/{companyId}/sites</c>.</summary>
public static class SiteEndpoints
{
    public static void MapSiteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients/{clientId:guid}/companies/{companyId:guid}/sites")
            .RequireAuthorization();

        group.MapGet("", ListSitesAsync);
        group.MapGet("/{siteId:guid}", GetSiteAsync);

        group.MapPost("", CreateSiteAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{siteId:guid}", UpdateSiteAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPatch("/{siteId:guid}/status", ChangeSiteStatusAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
    }

    private static async Task<Results<Ok<SiteListResponse>, NotFound>> ListSitesAsync(
        Guid clientId, Guid companyId, ListSitesQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListSitesQuery(clientId, companyId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        var items = result.Items.Select(s => new SiteListItemResponse(s.Id, s.Name, s.City, s.IsActive)).ToArray();
        return TypedResults.Ok(new SiteListResponse(items));
    }

    private static async Task<Results<Ok<SiteDetailResponse>, NotFound>> GetSiteAsync(
        Guid clientId, Guid companyId, Guid siteId, GetSiteQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSiteQuery(clientId, companyId, siteId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new SiteDetailResponse(
            result.Id, result.AuditedCompanyId, result.Name, result.AddressLine1, result.AddressLine2, result.City,
            result.StateRegion, result.PostalCode, result.Country, result.Latitude, result.Longitude,
            result.IsActive, result.CreatedAtUtc, result.UpdatedAtUtc));
    }

    private static async Task<Results<Created<CreateSiteResponse>, NotFound, ValidationProblem>> CreateSiteAsync(
        Guid clientId, Guid companyId, CreateSiteRequest request, CreateSiteCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateSiteCommand(
            clientId, companyId, request.Name, request.AddressLine1, request.AddressLine2, request.City,
            request.StateRegion, request.PostalCode, request.Country, request.Latitude, request.Longitude);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == CreateSiteError.CompanyNotFound)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = [result.ErrorDetail ?? "Request inválido."],
            });
        }

        var response = new CreateSiteResponse(result.SiteId!.Value);
        return TypedResults.Created($"/api/clients/{clientId}/companies/{companyId}/sites/{response.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateSiteAsync(
        Guid clientId, Guid companyId, Guid siteId, UpdateSiteRequest request, UpdateSiteCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSiteCommand(
            clientId, companyId, siteId, request.Name, request.AddressLine1, request.AddressLine2, request.City,
            request.StateRegion, request.PostalCode, request.Country, request.Latitude, request.Longitude);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error == UpdateSiteError.NotFound)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = [result.ErrorDetail ?? "Request inválido."],
            });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> ChangeSiteStatusAsync(
        Guid clientId, Guid companyId, Guid siteId, ChangeStatusRequest request,
        ChangeSiteStatusCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ChangeSiteStatusCommand(clientId, companyId, siteId, request.IsActive), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
