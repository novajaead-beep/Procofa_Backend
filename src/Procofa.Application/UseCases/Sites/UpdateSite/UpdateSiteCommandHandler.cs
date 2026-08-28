using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Sites.UpdateSite;

public sealed class UpdateSiteCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditedCompanyRepository auditedCompanyRepository,
    ICompanySiteRepository companySiteRepository)
{
    private const string DefaultCountry = "México";

    public Task<UpdateSiteResult> HandleAsync(UpdateSiteCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Task.FromResult(UpdateSiteResult.Failure(UpdateSiteError.ValidationFailed, "name es obligatorio."));
        }

        if (string.IsNullOrWhiteSpace(command.AddressLine1))
        {
            return Task.FromResult(
                UpdateSiteResult.Failure(UpdateSiteError.ValidationFailed, "addressLine1 es obligatorio."));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<UpdateSiteResult> ExecuteAsync(Guid tenantId, UpdateSiteCommand command, CancellationToken ct)
    {
        var company = await auditedCompanyRepository.GetByIdAsync(tenantId, command.ClientId, command.CompanyId, ct);
        if (company is null)
        {
            return UpdateSiteResult.Failure(UpdateSiteError.NotFound);
        }

        var site = await companySiteRepository.GetByIdAsync(tenantId, command.CompanyId, command.SiteId, ct);
        if (site is null)
        {
            return UpdateSiteResult.Failure(UpdateSiteError.NotFound);
        }

        site.UpdateDetails(
            command.Name!, command.AddressLine1!, command.AddressLine2, command.City, command.StateRegion,
            command.PostalCode, string.IsNullOrWhiteSpace(command.Country) ? DefaultCountry : command.Country,
            command.Latitude, command.Longitude);

        return UpdateSiteResult.Success();
    }
}
