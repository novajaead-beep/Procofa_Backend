using Procofa.Application.Ports.Out.Evidence;

namespace Procofa.Application.Ports.Out;

/// <summary>
/// Puerto de salida para almacenamiento de evidencias (fotos/documentos, HU-12 a HU-15).
/// Contrato deliberadamente sin operaciones de Update/Delete: el almacenamiento es aditivo e
/// inmutable — una nueva carga sobre el mismo criterio genera un nuevo StorageKey versionado,
/// nunca sobrescribe el binario original (HU-13).
/// </summary>
public interface IEvidenceStoragePort
{
    /// <summary>Sube el archivo y calcula su hash de integridad. Debe validar tipo MIME real (no solo extensión).</summary>
    Task<EvidenceStorageResult> UploadAsync(EvidenceUploadRequest request, CancellationToken cancellationToken);

    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken);
}
