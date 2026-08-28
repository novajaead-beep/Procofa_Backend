using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Clients;
using Procofa.Api.Contracts.Companies;
using Procofa.Application.UseCases.Companies.ChangeCompanyStatus;
using Procofa.Application.UseCases.Companies.CreateCompany;
using Procofa.Application.UseCases.Companies.GetCompany;
using Procofa.Application.UseCases.Companies.ListCompanies;
using Procofa.Application.UseCases.Companies.UpdateCompany;
using Procofa.Application.UseCases.Users;

namespace Procofa.Api.Endpoints;

/// <summary>Endpoints de <c>/api/clients/{clientId}/companies</c>. Mismo criterio de autorización
/// que <see cref="ClientEndpoints"/>: lectura para cualquier rol autenticado (con alcance de
/// CLIENTE), escritura exclusiva de ADMIN.</summary>
public static class CompanyEndpoints
{
    public static void MapCompanyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients/{clientId:guid}/companies").RequireAuthorization();

        group.MapGet("", ListCompaniesAsync);
        group.MapGet("/{companyId:guid}", GetCompanyAsync);

        group.MapPost("", CreateCompanyAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{companyId:guid}", UpdateCompanyAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPatch("/{companyId:guid}/status", ChangeCompanyStatusAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
    }

    private static async Task<Results<Ok<CompanyListResponse>, NotFound>> ListCompaniesAsync(
        Guid clientId, string? search, bool? isActive, int? page, int? pageSize,
        ListCompaniesQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ListCompaniesQuery(clientId, search, isActive, page ?? 1, pageSize ?? 25), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        var items = result.Items
            .Select(c => new CompanyListItemResponse(c.Id, c.LegalName, c.TradeName, c.TaxId, c.IsClientCompany, c.IsActive, c.CreatedAtUtc))
            .ToArray();

        return TypedResults.Ok(new CompanyListResponse(items, result.Page, result.PageSize, result.Total));
    }

    private static async Task<Results<Ok<CompanyDetailResponse>, NotFound>> GetCompanyAsync(
        Guid clientId, Guid companyId, GetCompanyQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCompanyQuery(clientId, companyId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new CompanyDetailResponse(
            result.Id, result.ClientId, result.DefaultProfileId, result.LegalName, result.TradeName, result.TaxId,
            result.Industry, result.CompanyType, result.IsClientCompany, result.IsActive, result.CreatedAtUtc,
            result.UpdatedAtUtc));
    }

    private static async Task<Results<Created<CreateCompanyResponse>, NotFound, ValidationProblem, ProblemHttpResult>> CreateCompanyAsync(
        Guid clientId, CreateCompanyRequest request, CreateCompanyCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateCompanyCommand(
            clientId, request.DefaultProfileId, request.LegalName, request.TradeName, request.TaxId,
            request.Industry, request.CompanyType, request.IsClientCompany);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                CreateCompanyError.ClientNotFound => TypedResults.NotFound(),
                CreateCompanyError.TaxIdAlreadyExists => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "El tax_id ya está en uso para este cliente.",
                    detail: result.ErrorDetail),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        var response = new CreateCompanyResponse(result.CompanyId!.Value);
        return TypedResults.Created($"/api/clients/{clientId}/companies/{response.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> UpdateCompanyAsync(
        Guid clientId, Guid companyId, UpdateCompanyRequest request, UpdateCompanyCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCompanyCommand(
            clientId, companyId, request.DefaultProfileId, request.LegalName, request.TradeName, request.TaxId,
            request.Industry, request.CompanyType, request.IsClientCompany);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error switch
            {
                UpdateCompanyError.NotFound => TypedResults.NotFound(),
                UpdateCompanyError.TaxIdAlreadyExists => TypedResults.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "El tax_id ya está en uso para este cliente.",
                    detail: result.ErrorDetail),
                _ => TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["request"] = [result.ErrorDetail ?? "Request inválido."],
                }),
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> ChangeCompanyStatusAsync(
        Guid clientId, Guid companyId, ChangeStatusRequest request, ChangeCompanyStatusCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ChangeCompanyStatusCommand(clientId, companyId, request.IsActive), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
