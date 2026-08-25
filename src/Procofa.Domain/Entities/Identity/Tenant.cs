namespace Procofa.Domain.Entities.Identity;

/// <summary>
/// Organización dueña del sistema PROCOFA. Aggregate Root.
/// Tabla física: <c>tenants</c> (1 fila sembrada en Etapa 1,
/// <c>id = 00000000-0000-0000-0000-000000000001</c>, <c>slug = 'procofa'</c>).
///
/// <c>Tenant</c> ≠ <c>Client</c>: Tenant es la organización dueña del
/// sistema (multitenant real a nivel físico, aunque Etapa 1 use un único
/// tenant fijo); Client es quien contrata el servicio de auditoría.
///
/// La policy RLS de esta tabla es auto-referencial:
/// <c>tenants_isolation USING (id = current_setting('app.tenant_id')::uuid)</c> —
/// sin <c>SET LOCAL app.tenant_id</c> previo no se puede leer ni la propia
/// fila del tenant.
/// </summary>
public sealed class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? LegalName { get; private set; }
    public string? TaxId { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>Column default <c>now()</c> en INSERT; sin trigger de UPDATE.</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Mantenido por el trigger <c>trg_tenants_updated_at</c>
    /// (<c>set_updated_at_utc()</c>) en cada UPDATE. EF nunca la escribe.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private Tenant() { }

    public Tenant(Guid id, string name, string slug, string? legalName, string? taxId)
    {
        Id = id;
        Name = name;
        Slug = slug;
        LegalName = legalName;
        TaxId = taxId;
        IsActive = true;
    }
}
