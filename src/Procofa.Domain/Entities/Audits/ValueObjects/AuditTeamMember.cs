using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Audits.ValueObjects;

/// <summary>
/// Asignación de un usuario al equipo auditor de una <see cref="Audit"/>
/// concreta, con su <see cref="AuditRole"/> (LEAD/SUPPORT) — independiente
/// del rol de sistema del usuario.
/// Tabla física: <c>audit_team</c> — PK compuesta <c>(audit_id, user_id)</c>,
/// tenant-scoped, sin columna <c>id</c>. Colección owned dentro de
/// <see cref="Audit.Team"/>, sin <c>DbSet</c> propio.
///
/// Un solo <see cref="AuditTeamRole.Lead"/> por auditoría se garantiza en BD
/// vía <c>uq_audit_team_one_lead ON audit_team(audit_id) WHERE audit_role = 'LEAD'</c>
/// — ver <c>AuditConfiguration</c>.
/// </summary>
public sealed class AuditTeamMember
{
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid UserId { get; private set; }
    public AuditTeamRole AuditRole { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private AuditTeamMember() { }

    public AuditTeamMember(Guid tenantId, Guid auditId, Guid userId, AuditTeamRole auditRole, Guid? assignedByUserId)
    {
        TenantId = tenantId;
        AuditId = auditId;
        UserId = userId;
        AuditRole = auditRole;
        AssignedByUserId = assignedByUserId;
    }
}
