using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Audits;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Clients;

namespace Procofa.Application.UseCases.Audits.ListAudits;

/// <summary>Caso de uso <c>GET /api/audits</c>. Alcance de lectura resuelto por <see
/// cref="ClientAccessScope"/> (ADMIN/AUDITOR_LIDER/AUDITOR_APOYO/CONSULTOR: todo el tenant;
/// CLIENTE: solo sus clients asignados).</summary>
public sealed class ListAuditsQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditRepository auditRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public Task<ListAuditsResult> HandleAsync(ListAuditsQuery query, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(query.ExecutionMode) &&
            !ExecutionModeParser.TryParse(query.ExecutionMode, out _))
        {
            return Task.FromResult(ListAuditsResult.Failure(
                ListAuditsError.ValidationFailed, "executionMode debe ser ONSITE, REMOTE o HYBRID."));
        }

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
                if (scope is not null && scope.Count == 0)
                {
                    // CLIENTE sin ningún client asignado: alcance vacío, se evita el viaje a BD.
                    return new ListAuditsResult([], page, pageSize, 0);
                }

                var pageResult = await auditRepository.ListAsync(
                    tenantId, query.ClientId, query.CompanyId, query.Status, query.AuditTypeId,
                    query.ExecutionMode, query.Search, page, pageSize, scope, ct);

                var items = pageResult.Items
                    .Select(a => new AuditListItem(
                        a.Id, a.Folio, a.ClientId, a.AuditedCompanyId, a.CompanySiteId, a.AuditTypeId, a.ProfileId,
                        a.StatusId, a.Objective, a.ScheduledDate, a.StartedAtUtc, a.ExecutionMode, a.CreatedAtUtc))
                    .ToArray();

                return new ListAuditsResult(items, page, pageSize, pageResult.Total);
            },
            cancellationToken);
    }
}
