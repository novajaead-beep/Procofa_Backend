namespace Procofa.Application.Ports.Out.Reports;

/// <summary>
/// Solicitud de generación de reporte (HU-21/HU-22). TemplateVersionId fija la plantilla vigente
/// utilizada, de forma que el reporte generado quede versionado y no se altere si la plantilla
/// administrable cambia después (HU-23/HU-24).
/// </summary>
public sealed record ReportGenerationRequest(
    Guid AuditPlanId,
    ReportFormat Format,
    Guid TemplateVersionId,
    Guid RequestedByUserId);
