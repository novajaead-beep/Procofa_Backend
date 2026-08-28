using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Clients.ListClients;

/// <summary>Caso de uso <c>GET /api/clients</c>. Alcance de lectura resuelto por <see
/// cref="ClientAccessScope"/> (ADMIN/AUDITOR_LIDER/AUDITOR_APOYO/CONSULTOR: todo el tenant; CLIENTE:
/// solo sus clients asignados).</summary>
public sealed class ListClientsQueryHandler(
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

    public Task<ListClientsResult> HandleAsync(ListClientsQuery query, CancellationToken cancellationToken)
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

                var pageResult = await clientRepository.ListAsync(
                    tenantId, query.Search, query.IsActive, query.Program, scope, page, pageSize, ct);

                var companyCounts = await auditedCompanyRepository.CountByClientIdsAsync(
                    tenantId, pageResult.Items.Select(c => c.Id).ToArray(), ct);

                var items = pageResult.Items
                    .Select(c => new ClientListItem(
                        c.Id, c.LegalName, c.TradeName, c.TaxId, c.IsActive, c.ProgramCodes,
                        companyCounts.GetValueOrDefault(c.Id, 0), c.CreatedAtUtc))
                    .ToArray();

                return new ListClientsResult(items, page, pageSize, pageResult.Total);
            },
            cancellationToken);
    }
}
