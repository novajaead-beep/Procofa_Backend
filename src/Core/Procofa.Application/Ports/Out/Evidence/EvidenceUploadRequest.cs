namespace Procofa.Application.Ports.Out.Evidence;

/// <summary>Solicitud de carga de evidencia (HU-12/HU-13). El adaptador valida MIME real, no solo extensión.</summary>
public sealed record EvidenceUploadRequest(
    Guid AuditPlanId,
    Guid CriterionSnapshotId,
    Guid UploadedByUserId,
    string FileName,
    string DeclaredContentType,
    Stream Content,
    long SizeBytes);
