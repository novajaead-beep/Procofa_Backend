using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="ReportFormat"/> ↔
/// <c>audit_reports.format varchar(10)</c> (constraint
/// <c>audit_reports_format_check</c>) — Instrucción 03.1, defecto 1.
/// </summary>
public sealed class ReportFormatConverter : ValueConverter<ReportFormat, string>
{
    public ReportFormatConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(ReportFormat value) => value switch
    {
        ReportFormat.Pdf => "PDF",
        ReportFormat.Docx => "DOCX",
        ReportFormat.Xlsx => "XLSX",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(ReportFormat)} sin mapeo físico explícito."),
    };

    private static ReportFormat FromDb(string value) => value switch
    {
        "PDF" => ReportFormat.Pdf,
        "DOCX" => ReportFormat.Docx,
        "XLSX" => ReportFormat.Xlsx,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(ReportFormat)}."),
    };
}
