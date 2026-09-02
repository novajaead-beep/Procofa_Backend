namespace Procofa.Api.Contracts.Audits;

public sealed record ReplaceAuditProgramsRequest(IReadOnlyCollection<string>? ProgramCodes);
