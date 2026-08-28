using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Clients;

namespace Procofa.Application.UseCases.Sites.ListSites;

public sealed class ListSitesQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditedCompanyRepository auditedCompanyRepository,
    ICompanySiteRepository companySiteRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    public Task<ListSitesResult> HandleAsync(ListSitesQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var scope = await ClientAccessScope.ResolveAsync(currentUser, tenantId, userRepository, ct);
                if (!ClientAccessScope.IsVisible(scope, query.ClientId))
                {
                    return ListSitesResult.Failure(ListSitesError.CompanyNotFound);
                }

                var company = await auditedCompanyRepository.GetByIdAsync(tenantId, query.ClientId, query.CompanyId, ct);
                if (company is null)
                {
                    return ListSitesResult.Failure(ListSitesError.CompanyNotFound);
                }

                var sites = await companySiteRepository.ListByCompanyAsync(tenantId, query.CompanyId, ct);

                var items = sites.Select(s => new SiteListItem(s.Id, s.Name, s.City, s.IsActive)).ToArray();
                return ListSitesResult.Success(items);
            },
            cancellationToken);
    }
}
