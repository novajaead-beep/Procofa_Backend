namespace Procofa.Api.Contracts.ChecklistSections;

public sealed record ChecklistSectionListItemResponse(
    Guid Id, string? Code, string Name, string? Description, int SortOrder);

public sealed record ChecklistSectionListResponse(IReadOnlyCollection<ChecklistSectionListItemResponse> Items);
