namespace Procofa.Application.UseCases.ChecklistVersions.CreateChecklistVersion;

public sealed record CreateChecklistVersionCommand(Guid ChecklistId, string? ChangeNotes);
