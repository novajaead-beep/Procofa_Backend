using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Clients;

namespace Procofa.Application.UseCases.Contacts.GetContact;

public sealed class GetContactQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientContactRepository clientContactRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    public Task<GetContactResult> HandleAsync(GetContactQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var scope = await ClientAccessScope.ResolveAsync(currentUser, tenantId, userRepository, ct);
                if (!ClientAccessScope.IsVisible(scope, query.ClientId))
                {
                    return GetContactResult.NotFound();
                }

                var contact = await clientContactRepository.GetByIdAsync(tenantId, query.ClientId, query.ContactId, ct);
                if (contact is null)
                {
                    return GetContactResult.NotFound();
                }

                return GetContactResult.Success(
                    contact.Id, contact.ClientId, contact.AuditedCompanyId, contact.FirstName, contact.LastName,
                    contact.JobTitle, contact.Email, contact.Phone, contact.IsActive, contact.CreatedAtUtc,
                    contact.UpdatedAtUtc);
            },
            cancellationToken);
    }
}
