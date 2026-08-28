using Microsoft.AspNetCore.Http.HttpResults;
using Procofa.Api.Contracts.Clients;
using Procofa.Api.Contracts.Contacts;
using Procofa.Application.UseCases.Contacts.ChangeContactStatus;
using Procofa.Application.UseCases.Contacts.CreateContact;
using Procofa.Application.UseCases.Contacts.GetContact;
using Procofa.Application.UseCases.Contacts.ListContacts;
using Procofa.Application.UseCases.Contacts.UpdateContact;
using Procofa.Application.UseCases.Users;

namespace Procofa.Api.Endpoints;

/// <summary>Endpoints de <c>/api/clients/{clientId}/contacts</c>. El status endpoint existe porque
/// el baseline tiene <c>client_contacts.is_active</c> — borrado lógico vía activar/desactivar, sin
/// hard delete.</summary>
public static class ContactEndpoints
{
    public static void MapContactEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clients/{clientId:guid}/contacts").RequireAuthorization();

        group.MapGet("", ListContactsAsync);
        group.MapGet("/{contactId:guid}", GetContactAsync);

        group.MapPost("", CreateContactAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPut("/{contactId:guid}", UpdateContactAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
        group.MapPatch("/{contactId:guid}/status", ChangeContactStatusAsync)
            .RequireAuthorization(policy => policy.RequireRole(UserRoleCodes.Admin));
    }

    private static async Task<Results<Ok<ContactListResponse>, NotFound>> ListContactsAsync(
        Guid clientId, ListContactsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListContactsQuery(clientId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        var items = result.Items
            .Select(c => new ContactListItemResponse(c.Id, c.FirstName, c.LastName, c.JobTitle, c.Email, c.Phone, c.IsActive))
            .ToArray();

        return TypedResults.Ok(new ContactListResponse(items));
    }

    private static async Task<Results<Ok<ContactDetailResponse>, NotFound>> GetContactAsync(
        Guid clientId, Guid contactId, GetContactQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContactQuery(clientId, contactId), cancellationToken);
        if (!result.IsSuccess)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new ContactDetailResponse(
            result.Id, result.ClientId, result.AuditedCompanyId, result.FirstName, result.LastName,
            result.JobTitle, result.Email, result.Phone, result.IsActive, result.CreatedAtUtc,
            result.UpdatedAtUtc));
    }

    private static async Task<Results<Created<CreateContactResponse>, NotFound, ValidationProblem>> CreateContactAsync(
        Guid clientId, CreateContactRequest request, CreateContactCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateContactCommand(
            clientId, request.AuditedCompanyId, request.FirstName, request.LastName, request.JobTitle,
            request.Email, request.Phone);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error is CreateContactError.ClientNotFound or CreateContactError.CompanyNotFound)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["request"] = [result.ErrorDetail ?? "Request inválido."],
            });
        }

        var response = new CreateContactResponse(result.ContactId!.Value);
        return TypedResults.Created($"/api/clients/{clientId}/contacts/{response.Id}", response);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateContactAsync(
        Guid clientId, Guid contactId, UpdateContactRequest request, UpdateContactCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateContactCommand(
            clientId, contactId, request.AuditedCompanyId, request.FirstName, request.LastName, request.JobTitle,
            request.Email, request.Phone);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error is UpdateContactError.NotFound or UpdateContactError.CompanyNotFound)
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

    private static async Task<Results<NoContent, NotFound>> ChangeContactStatusAsync(
        Guid clientId, Guid contactId, ChangeStatusRequest request, ChangeContactStatusCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ChangeContactStatusCommand(clientId, contactId, request.IsActive), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
