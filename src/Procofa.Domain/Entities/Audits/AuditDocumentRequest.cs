using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Audits;

/// <summary>
/// Solicitud de documento hecha al cliente/auditado dentro de una
/// <see cref="Audit"/>. Entidad independiente con <c>DbSet</c> propio —
/// referenciada externamente por <c>audit_evidences.document_request_id</c>.
/// Tabla física: <c>audit_document_requests</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>audits</c>.
/// </summary>
public sealed class AuditDocumentRequest
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DocumentRequestStatus Status { get; private set; } = DocumentRequestStatus.Pendiente;
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_document_requests_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private AuditDocumentRequest() { }

    public AuditDocumentRequest(
        Guid id,
        Guid tenantId,
        Guid auditId,
        Guid requestedByUserId,
        string title,
        string? description,
        DateOnly? dueDate)
    {
        Id = id;
        TenantId = tenantId;
        AuditId = auditId;
        RequestedByUserId = requestedByUserId;
        Title = title;
        Description = description;
        DueDate = dueDate;
        Status = DocumentRequestStatus.Pendiente;
    }
}
