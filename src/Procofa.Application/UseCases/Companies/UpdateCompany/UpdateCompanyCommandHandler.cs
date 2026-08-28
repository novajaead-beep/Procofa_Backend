using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Companies.UpdateCompany;

public sealed class UpdateCompanyCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditedCompanyRepository auditedCompanyRepository)
{
    public Task<UpdateCompanyResult> HandleAsync(UpdateCompanyCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.LegalName))
        {
            return Task.FromResult(
                UpdateCompanyResult.Failure(UpdateCompanyError.ValidationFailed, "legalName es obligatorio."));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<UpdateCompanyResult> ExecuteAsync(
        Guid tenantId, UpdateCompanyCommand command, CancellationToken ct)
    {
        var company = await auditedCompanyRepository.GetByIdAsync(tenantId, command.ClientId, command.CompanyId, ct);
        if (company is null)
        {
            return UpdateCompanyResult.Failure(UpdateCompanyError.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(command.TaxId) &&
            await auditedCompanyRepository.ExistsByTaxIdAsync(
                tenantId, command.ClientId, command.TaxId, company.Id, ct))
        {
            return UpdateCompanyResult.Failure(
                UpdateCompanyError.TaxIdAlreadyExists,
                "Ya existe otra empresa auditada con ese tax_id para este cliente.");
        }

        company.UpdateDetails(
            command.DefaultProfileId, command.LegalName!, command.TradeName, command.TaxId, command.Industry,
            command.CompanyType, command.IsClientCompany);

        return UpdateCompanyResult.Success();
    }
}
