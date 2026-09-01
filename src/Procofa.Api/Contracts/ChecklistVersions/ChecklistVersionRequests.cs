namespace Procofa.Api.Contracts.ChecklistVersions;

public sealed record CreateChecklistVersionRequest(string? ChangeNotes);

public sealed record UpdateChecklistVersionRequest(string? ChangeNotes);

public sealed record CreateChecklistVersionResponse(Guid Id, int VersionNumber);
