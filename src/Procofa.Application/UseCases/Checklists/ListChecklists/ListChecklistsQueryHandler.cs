using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Checklists.ListChecklists;

public sealed class ListChecklistsQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistRepository checklistRepository)
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public Task<ListChecklistsResult> HandleAsync(ListChecklistsQuery query, CancellationToken cancellationToken)
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
                var pageResult = await checklistRepository.ListAsync(
                    tenantId, query.Search, query.ProgramId, query.ProfileId, query.AuditTypeId, query.IsActive,
                    page, pageSize, ct);

                var items = pageResult.Items
                    .Select(c => new ChecklistListItem(
                        c.Id, c.ProgramId, c.ProfileId, c.AuditTypeId, c.Name, c.Description, c.IsActive,
                        c.CreatedAtUtc))
                    .ToArray();

                return new ListChecklistsResult(items, page, pageSize, pageResult.Total);
            },
            cancellationToken);
    }
}
