namespace Procofa.Application.UseCases.ChecklistVersions.GetChecklistVersion;

public sealed record GetChecklistVersionQuery(Guid ChecklistId, Guid VersionId);
