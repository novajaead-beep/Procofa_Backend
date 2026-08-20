using Procofa.Application.Ports.Out;
using Procofa.Application.Ports.Out.Reports;

namespace Procofa.Adapters.ReportGeneration;

/// <summary>Implementación con OpenXML SDK (Word) + motor de conversión a PDF. Adaptador secundario (driven).</summary>
public sealed class OpenXmlReportGenerator : IReportGeneratorPort
{
    public Task<GeneratedReport> GenerateAsync(ReportGenerationRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException(
            "Composición de portada, resultados por sección, hallazgos y evidencia — Módulo de Reportes, Semana 10 (HU-21/HU-22).");
}
