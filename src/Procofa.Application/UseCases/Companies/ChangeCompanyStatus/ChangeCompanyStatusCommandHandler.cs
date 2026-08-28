using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Companies.ChangeCompanyStatus;

public sealed class ChangeCompanyStatusCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditedCompanyRepository auditedCompanyRepository)
{
    public Task<ChangeCompanyStatusResult> HandleAsync(
        ChangeCompanyStatusCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var company = await auditedCompanyRepository.GetByIdAsync(
                    tenantId, command.ClientId, command.CompanyId, ct);
                if (company is null)
                {
                    return ChangeCompanyStatusResult.Failure(ChangeCompanyStatusError.NotFound);
                }

                if (command.IsActive)
                {
                    company.Activate();
                }
                else
                {
                    company.Deactivate();
                }

                return ChangeCompanyStatusResult.Success();
            },
            cancellationToken);
    }
}
