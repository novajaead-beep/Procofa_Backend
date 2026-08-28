using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.UseCases.Contacts.CreateContact;

public sealed class CreateContactCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientRepository clientRepository,
    IAuditedCompanyRepository auditedCompanyRepository,
    IClientContactRepository clientContactRepository)
{
    public Task<CreateContactResult> HandleAsync(CreateContactCommand command, CancellationToken cancellationToken)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return Task.FromResult(CreateContactResult.Failure(CreateContactError.ValidationFailed, validationError));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<CreateContactResult> ExecuteAsync(
        Guid tenantId, CreateContactCommand command, CancellationToken ct)
    {
        var client = await clientRepository.GetByIdAsync(tenantId, command.ClientId, ct);
        if (client is null)
        {
            return CreateContactResult.Failure(CreateContactError.ClientNotFound);
        }

        if (command.AuditedCompanyId.HasValue)
        {
            var company = await auditedCompanyRepository.GetByIdAsync(
                tenantId, command.ClientId, command.AuditedCompanyId.Value, ct);
            if (company is null)
            {
                return CreateContactResult.Failure(CreateContactError.CompanyNotFound);
            }
        }

        var contact = new ClientContact(
            Guid.NewGuid(), tenantId, command.ClientId, command.AuditedCompanyId, command.FirstName!,
            command.LastName!, command.JobTitle, command.Email, command.Phone);

        await clientContactRepository.AddAsync(contact, ct);

        return CreateContactResult.Success(contact.Id);
    }

    private static string? Validate(CreateContactCommand command)
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
