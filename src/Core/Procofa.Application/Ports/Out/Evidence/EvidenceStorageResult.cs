namespace Procofa.Application.Ports.Out.Evidence;

/// <summary>
/// Resultado de una carga inmutable de evidencia (HU-13). StorageKey es la referencia opaca al
/// objeto en el almacenamiento (no editable); Sha256Hash permite verificar integridad/alteración.
/// </summary>
public sealed record EvidenceStorageResult(
    string StorageKey,
    string Sha256Hash,
    long SizeBytes,
    DateTime UploadedAtUtc);
