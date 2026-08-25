namespace Procofa.Domain.Enums;

/// <summary>
/// Estado de una solicitud de documento a un cliente/auditado.
/// Respaldado por <c>audit_document_requests.status varchar(30) DEFAULT 'PENDIENTE'</c>
/// con <c>CHECK (status IN
/// ('PENDIENTE','ENTREGADO','VALIDADO','RECHAZADO','CANCELADO'))</c>.
/// </summary>
public enum DocumentRequestStatus
{
    Pendiente,
    Entregado,
    Validado,
    Rechazado,
    Cancelado
}
