namespace Procofa.Application.UseCases.ChecklistSections.UpdateChecklistSection;

public sealed record UpdateChecklistSectionCommand(
    Guid ChecklistId, Guid VersionId, Guid SectionId, string? Code, string? Name, string? Description, int SortOrder);
