using Procofa.Domain.Enums;

namespace Procofa.Application.UseCases.Audits;

/// <summary>Traduce el string físico de <c>audit_team.audit_role</c> ("LEAD"/"SUPPORT") al enum de
/// dominio — <see cref="AuditTeamRole"/> es un valor cerrado propio de la auditoría, independiente
/// del rol de sistema del usuario (<c>UserRoleCodes</c>).</summary>
public static class AuditTeamRoleParser
{
    public static bool TryParse(string? value, out AuditTeamRole role)
    {
        switch (value)
        {
            case "LEAD":
                role = AuditTeamRole.Lead;
                return true;
            case "SUPPORT":
                role = AuditTeamRole.Support;
                return true;
            default:
                role = default;
                return false;
        }
    }

    public static string ToRequestString(AuditTeamRole role) => role switch
    {
        AuditTeamRole.Lead => "LEAD",
        AuditTeamRole.Support => "SUPPORT",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };
}
