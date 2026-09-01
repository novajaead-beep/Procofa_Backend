namespace Procofa.Application.UseCases.ChecklistVersions.PublishChecklistVersion;

public sealed record PublishChecklistVersionCommand(Guid ChecklistId, Guid VersionId);
