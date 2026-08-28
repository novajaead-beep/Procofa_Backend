using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Contacts.ChangeContactStatus;

/// <summary>Caso de uso <c>PATCH /api/clients/{clientId}/contacts/{contactId}/status</c> — el
/// borrado lógico de contactos se implementa vía <c>client_contacts.is_active</c>, activando o
/// desactivando (nunca hard delete).</summary>
public sealed class ChangeContactStatusCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientContactRepository clientContactRepository)
{
    public Task<ChangeContactStatusResult> HandleAsync(
        ChangeContactStatusCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var contact = await clientContactRepository.GetByIdAsync(
                    tenantId, command.ClientId, command.ContactId, ct);
                if (contact is null)
                {
                    return ChangeContactStatusResult.Failure(ChangeContactStatusError.NotFound);
                }

                if (command.IsActive)
                {
                    contact.Activate();
                }
                else
                {
                    contact.Deactivate();
                }

                return ChangeContactStatusResult.Success();
            },
            cancellationToken);
    }
}
