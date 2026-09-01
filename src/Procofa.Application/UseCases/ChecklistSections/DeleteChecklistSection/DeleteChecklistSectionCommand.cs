namespace Procofa.Application.UseCases.ChecklistSections.DeleteChecklistSection;

public sealed record DeleteChecklistSectionCommand(Guid ChecklistId, Guid VersionId, Guid SectionId);
