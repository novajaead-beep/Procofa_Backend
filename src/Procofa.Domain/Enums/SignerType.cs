namespace Procofa.Domain.Enums;

/// <summary>
/// Rol del firmante de una auditoría (<c>AuditSignatory</c>).
/// Respaldado por <c>audit_signatories.signer_type varchar(30)</c> con
/// <c>CHECK (signer_type IN
/// ('AUDITOR_LIDER','AUDITOR','CLIENTE','RESPONSABLE'))</c>.
/// </summary>
public enum SignerType
{
    AuditorLider,
    Auditor,
    Cliente,
    Responsable
}
