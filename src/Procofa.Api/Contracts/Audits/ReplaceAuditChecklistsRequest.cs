namespace Procofa.Api.Contracts.Audits;

public sealed record ReplaceAuditChecklistsRequest(IReadOnlyCollection<Guid>? ChecklistIds);
