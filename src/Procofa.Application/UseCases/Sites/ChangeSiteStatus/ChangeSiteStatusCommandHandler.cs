using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Sites.ChangeSiteStatus;

public sealed class ChangeSiteStatusCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditedCompanyRepository auditedCompanyRepository,
    ICompanySiteRepository companySiteRepository)
{
    public Task<ChangeSiteStatusResult> HandleAsync(
        ChangeSiteStatusCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var company = await auditedCompanyRepository.GetByIdAsync(
                    tenantId, command.ClientId, command.CompanyId, ct);
                if (company is null)
                {
                    return ChangeSiteStatusResult.Failure(ChangeSiteStatusError.NotFound);
                }

                var site = await companySiteRepository.GetByIdAsync(tenantId, command.CompanyId, command.SiteId, ct);
                if (site is null)
                {
                    return ChangeSiteStatusResult.Failure(ChangeSiteStatusError.NotFound);
                }

                if (command.IsActive)
                {
                    site.Activate();
                }
                else
                {
                    site.Deactivate();
                }

                return ChangeSiteStatusResult.Success();
            },
            cancellationToken);
    }
}
