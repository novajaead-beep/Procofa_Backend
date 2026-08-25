using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="AuditReportStatus"/> ↔
/// <c>audit_reports.status varchar(20)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class AuditReportStatusConverter : ValueConverter<AuditReportStatus, string>
{
    public AuditReportStatusConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(AuditReportStatus value) => value switch
    {
        AuditReportStatus.Draft => "DRAFT",
        AuditReportStatus.Final => "FINAL",
        AuditReportStatus.Void => "VOID",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(AuditReportStatus)} sin mapeo físico explícito."),
    };

    private static AuditReportStatus FromDb(string value) => value switch
    {
        "DRAFT" => AuditReportStatus.Draft,
        "FINAL" => AuditReportStatus.Final,
        "VOID" => AuditReportStatus.Void,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(AuditReportStatus)}."),
    };
}
