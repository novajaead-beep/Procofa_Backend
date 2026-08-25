namespace Procofa.Domain.Enums;

/// <summary>
/// Rol de un miembro del equipo auditor dentro de UNA auditoría concreta —
/// independiente del rol de sistema (<c>roles</c>/<c>Role</c>) del usuario.
/// Respaldado por <c>audit_team.audit_role varchar(20)</c> con
/// <c>CHECK (audit_role IN ('LEAD','SUPPORT'))</c>.
///
/// La unicidad de un solo <see cref="Lead"/> por auditoría se garantiza en BD
/// vía el índice único parcial <c>uq_audit_team_one_lead ON audit_team(audit_id)
/// WHERE audit_role = 'LEAD'</c> — ver <c>AuditConfiguration</c>
/// (<c>OwnsMany(x => x.Team)</c>).
/// </summary>
public enum AuditTeamRole
{
    Lead,
    Support
}
