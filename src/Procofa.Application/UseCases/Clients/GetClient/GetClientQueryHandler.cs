using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Clients.GetClient;

public sealed class GetClientQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientRepository clientRepository,
    IAuditedCompanyRepository auditedCompanyRepository,
    IProgramRepository programRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    public Task<GetClientResult> HandleAsync(GetClientQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var scope = await ClientAccessScope.ResolveAsync(currentUser, tenantId, userRepository, ct);
                if (!ClientAccessScope.IsVisible(scope, query.ClientId))
                {
                    return GetClientResult.NotFound();
                }

                var client = await clientRepository.GetByIdAsync(tenantId, query.ClientId, ct);
                if (client is null)
                {
                    return GetClientResult.NotFound();
                }

                var programCodes = await programRepository.GetCodesByIdsAsync(
                    client.Programs.Select(p => p.ProgramId).ToArray(), ct);

                var companyCounts = await auditedCompanyRepository.CountByClientIdsAsync(
                    tenantId, [client.Id], ct);

                return GetClientResult.Success(
                    client.Id, client.LegalName, client.TradeName, client.TaxId, client.Industry,
                    client.CompanyType, client.Notes, client.IsActive, programCodes,
                    companyCounts.GetValueOrDefault(client.Id, 0), client.CreatedAtUtc, client.UpdatedAtUtc);
            },
            cancellationToken);
    }
}
