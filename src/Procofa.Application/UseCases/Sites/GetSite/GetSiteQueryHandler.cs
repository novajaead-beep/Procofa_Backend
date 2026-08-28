using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Clients;

namespace Procofa.Application.UseCases.Sites.GetSite;

public sealed class GetSiteQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditedCompanyRepository auditedCompanyRepository,
    ICompanySiteRepository companySiteRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    public Task<GetSiteResult> HandleAsync(GetSiteQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var scope = await ClientAccessScope.ResolveAsync(currentUser, tenantId, userRepository, ct);
                if (!ClientAccessScope.IsVisible(scope, query.ClientId))
                {
                    return GetSiteResult.NotFound();
                }

                var company = await auditedCompanyRepository.GetByIdAsync(tenantId, query.ClientId, query.CompanyId, ct);
                if (company is null)
                {
                    return GetSiteResult.NotFound();
                }

                var site = await companySiteRepository.GetByIdAsync(tenantId, query.CompanyId, query.SiteId, ct);
                if (site is null)
                {
                    return GetSiteResult.NotFound();
                }

                return GetSiteResult.Success(
                    site.Id, site.AuditedCompanyId, site.Name, site.AddressLine1, site.AddressLine2, site.City,
                    site.StateRegion, site.PostalCode, site.Country, site.Latitude, site.Longitude, site.IsActive,
                    site.CreatedAtUtc, site.UpdatedAtUtc);
            },
            cancellationToken);
    }
}
