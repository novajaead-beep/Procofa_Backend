namespace Procofa.Domain.Enums;

/// <summary>
/// Tipo/clasificación de un reporte. Compartido intencionalmente entre
/// <c>ReportTemplate.ReportType</c> y <c>AuditReport.ReportType</c> — ambas
/// columnas físicas usan el mismo conjunto de valores porque describen el
/// mismo concepto de dominio (qué clase de documento es), a diferencia de
/// los enums de estado de versión (ver <see cref="ChecklistVersionStatus"/>
/// vs <see cref="ReportTemplateVersionStatus"/>), que se mantienen separados
/// aunque hoy compartan las mismas 3 palabras.
///
/// Respaldado por:
/// <c>report_templates_report_type_check</c> y
/// <c>audit_reports_report_type_check</c>, ambos
/// <c>CHECK (report_type IN ('FINAL','EJECUTIVO','HALLAZGOS','ACCIONES','SEGUIMIENTO'))</c>.
/// </summary>
public enum ReportType
{
    Final,
    Ejecutivo,
    Hallazgos,
    Acciones,
    Seguimiento
}
