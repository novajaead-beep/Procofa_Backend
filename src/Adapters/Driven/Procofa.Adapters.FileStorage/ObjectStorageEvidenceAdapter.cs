using Procofa.Application.Ports.Out;
using Procofa.Application.Ports.Out.Evidence;

namespace Procofa.Adapters.FileStorage;

public sealed class ObjectStorageEvidenceAdapter : IEvidenceStoragePort
{
    public Task<EvidenceStorageResult> UploadAsync(EvidenceUploadRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException("Validación de MIME real + cálculo SHA-256 + carga aditiva — Módulo de Evidencias, Semana 7 (HU-12/HU-13).");

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
