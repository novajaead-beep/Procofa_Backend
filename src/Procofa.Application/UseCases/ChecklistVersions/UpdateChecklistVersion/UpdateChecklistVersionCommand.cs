namespace Procofa.Application.UseCases.ChecklistVersions.UpdateChecklistVersion;

public sealed record UpdateChecklistVersionCommand(Guid ChecklistId, Guid VersionId, string? ChangeNotes);
