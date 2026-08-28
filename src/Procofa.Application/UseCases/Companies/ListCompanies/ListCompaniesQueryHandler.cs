using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Clients;

namespace Procofa.Application.UseCases.Companies.ListCompanies;

public sealed class ListCompaniesQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientRepository clientRepository,
    IAuditedCompanyRepository auditedCompanyRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public Task<ListCompaniesResult> HandleAsync(ListCompaniesQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;
        var page = query.Page < 1 ? DefaultPage : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize,
        };

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var scope = await ClientAccessScope.ResolveAsync(currentUser, tenantId, userRepository, ct);
                if (!ClientAccessScope.IsVisible(scope, query.ClientId))
                {
                    return ListCompaniesResult.Failure(ListCompaniesError.ClientNotFound);
                }

                var client = await clientRepository.GetByIdAsync(tenantId, query.ClientId, ct);
                if (client is null)
                {
                    return ListCompaniesResult.Failure(ListCompaniesError.ClientNotFound);
                }

                var pageResult = await auditedCompanyRepository.ListAsync(
                    tenantId, query.ClientId, query.Search, query.IsActive, page, pageSize, ct);

                var items = pageResult.Items
                    .Select(c => new CompanyListItem(
                        c.Id, c.LegalName, c.TradeName, c.TaxId, c.IsClientCompany, c.IsActive, c.CreatedAtUtc))
                    .ToArray();

                return ListCompaniesResult.Success(items, page, pageSize, pageResult.Total);
            },
            cancellationToken);
    }
}
