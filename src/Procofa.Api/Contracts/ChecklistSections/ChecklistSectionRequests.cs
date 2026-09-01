namespace Procofa.Api.Contracts.ChecklistSections;

public sealed record CreateChecklistSectionRequest(string? Code, string? Name, string? Description, int SortOrder);

public sealed record UpdateChecklistSectionRequest(string? Code, string? Name, string? Description, int SortOrder);

public sealed record CreateChecklistSectionResponse(Guid Id);
