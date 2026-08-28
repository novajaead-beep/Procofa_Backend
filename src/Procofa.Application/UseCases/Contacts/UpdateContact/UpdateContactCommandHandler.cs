using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Contacts.UpdateContact;

public sealed class UpdateContactCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditedCompanyRepository auditedCompanyRepository,
    IClientContactRepository clientContactRepository)
{
    public Task<UpdateContactResult> HandleAsync(UpdateContactCommand command, CancellationToken cancellationToken)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return Task.FromResult(UpdateContactResult.Failure(UpdateContactError.ValidationFailed, validationError));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<UpdateContactResult> ExecuteAsync(
        Guid tenantId, UpdateContactCommand command, CancellationToken ct)
    {
        var contact = await clientContactRepository.GetByIdAsync(tenantId, command.ClientId, command.ContactId, ct);
        if (contact is null)
        {
            return UpdateContactResult.Failure(UpdateContactError.NotFound);
        }

        if (command.AuditedCompanyId.HasValue)
        {
            var company = await auditedCompanyRepository.GetByIdAsync(
                tenantId, command.ClientId, command.AuditedCompanyId.Value, ct);
            if (company is null)
            {
                return UpdateContactResult.Failure(UpdateContactError.CompanyNotFound);
            }
        }

        contact.UpdateDetails(
            command.AuditedCompanyId, command.FirstName!, command.LastName!, command.JobTitle, command.Email,
            command.Phone);

        return UpdateContactResult.Success();
    }

    private static string? Validate(UpdateContactCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.FirstName))
        {
            return "firstName es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.LastName))
        {
            return "lastName es obligatorio.";
        }

        if (!string.IsNullOrWhiteSpace(command.Email) &&
            (!command.Email.Contains('@') || command.Email.Any(char.IsWhiteSpace)))
        {
            return "email no tiene un formato válido.";
        }

        return null;
    }
}
