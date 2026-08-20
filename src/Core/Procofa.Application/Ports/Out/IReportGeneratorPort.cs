using Procofa.Application.Ports.Out.Reports;

namespace Procofa.Application.Ports.Out;

/// <summary>
/// Puerto de salida para generación de reportes Word/PDF (HU-21/HU-22). El adaptador (OpenXML SDK +
/// motor de conversión a PDF) resuelve la plantilla corporativa vigente y compone portada, resultados
/// por sección, hallazgos y evidencia fotográfica embebida. Debe ejecutarse de forma asíncrona/en
/// background cuando exceda el umbral síncrono (>10s, HU-21).
/// </summary>
public interface IReportGeneratorPort
{
    Task<GeneratedReport> GenerateAsync(ReportGenerationRequest request, CancellationToken cancellationToken);
}
