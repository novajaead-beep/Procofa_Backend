using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Clients;

namespace Procofa.Application.UseCases.Contacts.ListContacts;

public sealed class ListContactsQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientRepository clientRepository,
    IClientContactRepository clientContactRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    public Task<ListContactsResult> HandleAsync(ListContactsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var scope = await ClientAccessScope.ResolveAsync(currentUser, tenantId, userRepository, ct);
                if (!ClientAccessScope.IsVisible(scope, query.ClientId))
                {
                    return ListContactsResult.Failure(ListContactsError.ClientNotFound);
                }

                var client = await clientRepository.GetByIdAsync(tenantId, query.ClientId, ct);
                if (client is null)
                {
                    return ListContactsResult.Failure(ListContactsError.ClientNotFound);
                }

                var contacts = await clientContactRepository.ListByClientAsync(tenantId, query.ClientId, ct);

                var items = contacts
                    .Select(c => new ContactListItem(c.Id, c.FirstName, c.LastName, c.JobTitle, c.Email, c.Phone, c.IsActive))
                    .ToArray();

                return ListContactsResult.Success(items);
            },
            cancellationToken);
    }
}
