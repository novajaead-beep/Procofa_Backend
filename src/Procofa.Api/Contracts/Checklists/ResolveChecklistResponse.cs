namespace Procofa.Api.Contracts.Checklists;

public sealed record ResolveChecklistResponse(
    Guid ChecklistId, string ChecklistName, Guid VersionId, int VersionNumber, bool IsExactMatch);
