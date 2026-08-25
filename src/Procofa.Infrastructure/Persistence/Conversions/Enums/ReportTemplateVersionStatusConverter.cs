using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="ReportTemplateVersionStatus"/> ↔
/// <c>report_template_versions.status varchar(20)</c> (Instrucción 03.1, defecto 1).
/// Deliberadamente separado de <see cref="ChecklistVersionStatusConverter"/>
/// aunque comparten hoy las mismas 3 cadenas — ver <see cref="ChecklistVersionStatus"/>.
/// </summary>
public sealed class ReportTemplateVersionStatusConverter : ValueConverter<ReportTemplateVersionStatus, string>
{
    public ReportTemplateVersionStatusConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(ReportTemplateVersionStatus value) => value switch
    {
        ReportTemplateVersionStatus.Draft => "DRAFT",
        ReportTemplateVersionStatus.Published => "PUBLISHED",
        ReportTemplateVersionStatus.Retired => "RETIRED",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(ReportTemplateVersionStatus)} sin mapeo físico explícito."),
    };

    private static ReportTemplateVersionStatus FromDb(string value) => value switch
    {
        "DRAFT" => ReportTemplateVersionStatus.Draft,
        "PUBLISHED" => ReportTemplateVersionStatus.Published,
        "RETIRED" => ReportTemplateVersionStatus.Retired,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(ReportTemplateVersionStatus)}."),
    };
}
