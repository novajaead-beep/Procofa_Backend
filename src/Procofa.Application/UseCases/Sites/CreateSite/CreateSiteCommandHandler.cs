using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.UseCases.Sites.CreateSite;

public sealed class CreateSiteCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditedCompanyRepository auditedCompanyRepository,
    ICompanySiteRepository companySiteRepository)
{
    private const string DefaultCountry = "México";

    public Task<CreateSiteResult> HandleAsync(CreateSiteCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Task.FromResult(CreateSiteResult.Failure(CreateSiteError.ValidationFailed, "name es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(command.AddressLine1))
        {
            return Task.FromResult(
                CreateSiteResult.Failure(CreateSiteError.ValidationFailed, "addressLine1 es obligatorio."));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<CreateSiteResult> ExecuteAsync(Guid tenantId, CreateSiteCommand command, CancellationToken ct)
    {
        var company = await auditedCompanyRepository.GetByIdAsync(tenantId, command.ClientId, command.CompanyId, ct);
        if (company is null)
        {
            return CreateSiteResult.Failure(CreateSiteError.CompanyNotFound);
        }

        var site = new CompanySite(
            Guid.NewGuid(), tenantId, command.CompanyId, command.Name!, command.AddressLine1!, command.AddressLine2,
            command.City, command.StateRegion, command.PostalCode,
            string.IsNullOrWhiteSpace(command.Country) ? DefaultCountry : command.Country, command.Latitude,
            command.Longitude);

        await companySiteRepository.AddAsync(site, ct);

        return CreateSiteResult.Success(site.Id);
    }
}
