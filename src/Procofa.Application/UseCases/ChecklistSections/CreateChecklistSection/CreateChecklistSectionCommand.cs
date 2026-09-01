namespace Procofa.Application.UseCases.ChecklistSections.CreateChecklistSection;

public sealed record CreateChecklistSectionCommand(
    Guid ChecklistId, Guid VersionId, string? Code, string? Name, string? Description, int SortOrder);
