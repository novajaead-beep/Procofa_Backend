using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.UseCases.Companies.CreateCompany;

/// <summary>Caso de uso <c>POST /api/clients/{clientId}/companies</c>.</summary>
public sealed class CreateCompanyCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientRepository clientRepository,
    IAuditedCompanyRepository auditedCompanyRepository)
{
    public Task<CreateCompanyResult> HandleAsync(CreateCompanyCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.LegalName))
        {
            return Task.FromResult(
                CreateCompanyResult.Failure(CreateCompanyError.ValidationFailed, "legalName es obligatorio."));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<CreateCompanyResult> ExecuteAsync(
        Guid tenantId, CreateCompanyCommand command, CancellationToken ct)
    {
        var client = await clientRepository.GetByIdAsync(tenantId, command.ClientId, ct);
        if (client is null)
        {
            return CreateCompanyResult.Failure(CreateCompanyError.ClientNotFound);
        }

        if (!string.IsNullOrWhiteSpace(command.TaxId) &&
            await auditedCompanyRepository.ExistsByTaxIdAsync(
                tenantId, command.ClientId, command.TaxId, excludeCompanyId: null, ct))
        {
            return CreateCompanyResult.Failure(
                CreateCompanyError.TaxIdAlreadyExists,
                "Ya existe una empresa auditada con ese tax_id para este cliente.");
        }

        var company = new AuditedCompany(
            Guid.NewGuid(), tenantId, command.ClientId, command.DefaultProfileId, command.LegalName!,
            command.TradeName, command.TaxId, command.Industry, command.CompanyType, command.IsClientCompany);

        await auditedCompanyRepository.AddAsync(company, ct);

        return CreateCompanyResult.Success(company.Id);
    }
}
