namespace Procofa.Application.UseCases.Checklists.ListChecklists;

public sealed record ChecklistListItem(
    Guid Id,
    Guid ProgramId,
    Guid ProfileId,
    Guid? AuditTypeId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed class ListChecklistsResult(IReadOnlyList<ChecklistListItem> items, int page, int pageSize, int total)
{
    public IReadOnlyList<ChecklistListItem> Items { get; } = items;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public int Total { get; } = total;
}
