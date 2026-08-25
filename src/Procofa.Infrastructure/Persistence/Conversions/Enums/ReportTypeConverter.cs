using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="ReportType"/> ↔ <c>report_type varchar</c>
/// — compartido intencionalmente entre <c>audit_reports.report_type</c>
/// (<c>audit_reports_report_type_check</c>) y
/// <c>report_templates.report_type</c> (<c>report_templates_report_type_check</c>),
/// mismas 5 cadenas físicas en ambas tablas. Instrucción 03.1, defecto 1.
/// </summary>
public sealed class ReportTypeConverter : ValueConverter<ReportType, string>
{
    public ReportTypeConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(ReportType value) => value switch
    {
        ReportType.Final => "FINAL",
        ReportType.Ejecutivo => "EJECUTIVO",
        ReportType.Hallazgos => "HALLAZGOS",
        ReportType.Acciones => "ACCIONES",
        ReportType.Seguimiento => "SEGUIMIENTO",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(ReportType)} sin mapeo físico explícito."),
    };

    private static ReportType FromDb(string value) => value switch
    {
        "FINAL" => ReportType.Final,
        "EJECUTIVO" => ReportType.Ejecutivo,
        "HALLAZGOS" => ReportType.Hallazgos,
        "ACCIONES" => ReportType.Acciones,
        "SEGUIMIENTO" => ReportType.Seguimiento,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(ReportType)}."),
    };
}
