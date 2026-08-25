namespace Procofa.Domain.Entities.Clients;

/// <summary>
/// Contacto de un <see cref="Client"/>, opcionalmente asociado a una
/// <see cref="AuditedCompany"/> concreta. Entidad independiente con
/// <c>DbSet</c> propio (ver justificación en <see cref="Client"/>) —
/// referenciada externamente por <c>audit_signatories.client_contact_id</c>,
/// <c>findings.responsible_contact_id</c>,
/// <c>corrective_actions.responsible_contact_id</c>.
/// Tabla física: <c>client_contacts</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>clients</c>,
/// <c>ON DELETE SET NULL</c> desde <c>audited_companies</c>.
/// </summary>
public sealed class ClientContact
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid? AuditedCompanyId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? JobTitle { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_client_contacts_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private ClientContact() { }

    public ClientContact(
        Guid id,
        Guid tenantId,
        Guid clientId,
        Guid? auditedCompanyId,
        string firstName,
        string lastName,
        string? jobTitle,
        string? email,
        string? phone)
    {
        Id = id;
        TenantId = tenantId;
        ClientId = clientId;
        AuditedCompanyId = auditedCompanyId;
        FirstName = firstName;
        LastName = lastName;
        JobTitle = jobTitle;
        Email = email;
        Phone = phone;
        IsActive = true;
    }
}
