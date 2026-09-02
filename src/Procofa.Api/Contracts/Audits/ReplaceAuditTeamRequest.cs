namespace Procofa.Api.Contracts.Audits;

public sealed record AuditTeamMemberRequest(Guid UserId, string Role);

public sealed record ReplaceAuditTeamRequest(IReadOnlyCollection<AuditTeamMemberRequest>? Members);
