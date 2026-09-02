namespace Procofa.Application.UseCases.Audits.ReplaceAuditTeam;

public sealed record ReplaceAuditTeamMemberInput(Guid UserId, string Role);

/// <summary><c>PUT /api/audits/{auditId}/team</c>. <see cref="ReplaceAuditTeamMemberInput.Role"/>
/// es el valor físico cerrado de <c>audit_team.audit_role</c> ("LEAD"/"SUPPORT") — independiente
/// del rol de sistema del usuario.</summary>
public sealed record ReplaceAuditTeamCommand(Guid AuditId, IReadOnlyCollection<ReplaceAuditTeamMemberInput>? Members);
