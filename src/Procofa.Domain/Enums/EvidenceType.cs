namespace Procofa.Domain.Enums;

/// <summary>
/// Tipo físico de una evidencia cargada (<c>AuditEvidence</c>).
/// Respaldado por <c>audit_evidences.evidence_type varchar(30)</c> con
/// <c>CHECK (evidence_type IN
/// ('FOTO','PDF','WORD','EXCEL','IMAGEN','CAPTURA','REGISTRO','OTRO'))</c>.
/// </summary>
public enum EvidenceType
{
    Foto,
    Pdf,
    Word,
    Excel,
    Imagen,
    Captura,
    Registro,
    Otro
}
