namespace Procofa.Application.Ports.Out.Reports;

/// <summary>Reporte generado, listo para persistirse de forma inmutable (HU-24: no se regenera, se re-descarga).</summary>
public sealed record GeneratedReport(
    string FileName,
    string ContentType,
    byte[] Content,
    Guid TemplateVersionId,
    DateTime GeneratedAtUtc);
