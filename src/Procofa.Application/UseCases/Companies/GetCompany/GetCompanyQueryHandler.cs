using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Clients;

namespace Procofa.Application.UseCases.Companies.GetCompany;

public sealed class GetCompanyQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditedCompanyRepository auditedCompanyRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    public Task<GetCompanyResult> HandleAsync(GetCompanyQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var scope = await ClientAccessScope.ResolveAsync(currentUser, tenantId, userRepository, ct);
                if (!ClientAccessScope.IsVisible(scope, query.ClientId))
                {
                    return GetCompanyResult.NotFound();
                }

                var company = await auditedCompanyRepository.GetByIdAsync(tenantId, query.ClientId, query.CompanyId, ct);
                if (company is null)
                {
                    return GetCompanyResult.NotFound();
                }

                return GetCompanyResult.Success(
                    company.Id, company.ClientId, company.DefaultProfileId, company.LegalName, company.TradeName,
                    company.TaxId, company.Industry, company.CompanyType, company.IsClientCompany, company.IsActive,
                    company.CreatedAtUtc, company.UpdatedAtUtc);
            },
            cancellationToken);
    }
}
