using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="AuditTeamRole"/> ↔
/// <c>audit_team.audit_role varchar(20)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class AuditTeamRoleConverter : ValueConverter<AuditTeamRole, string>
{
    public AuditTeamRoleConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(AuditTeamRole value) => value switch
    {
        AuditTeamRole.Lead => "LEAD",
        AuditTeamRole.Support => "SUPPORT",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(AuditTeamRole)} sin mapeo físico explícito."),
    };

    private static AuditTeamRole FromDb(string value) => value switch
    {
        "LEAD" => AuditTeamRole.Lead,
        "SUPPORT" => AuditTeamRole.Support,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(AuditTeamRole)}."),
    };
}
