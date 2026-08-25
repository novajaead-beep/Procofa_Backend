namespace Procofa.Domain.Enums;

/// <summary>
/// Ciclo de vida de una <c>ReportTemplateVersion</c>.
/// Respaldado por <c>report_template_versions.status varchar(20) DEFAULT 'DRAFT'</c>
/// con <c>CHECK (status IN ('DRAFT','PUBLISHED','RETIRED'))</c>.
///
/// Ver <see cref="ChecklistVersionStatus"/> para la justificación de por qué
/// este enum NO se comparte con esa otra columna pese a tener las mismas
/// 3 cadenas físicas.
/// </summary>
public enum ReportTemplateVersionStatus
{
    Draft,
    Published,
    Retired
}
