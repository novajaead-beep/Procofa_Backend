namespace Procofa.Domain.Entities.Identity.ValueObjects;

/// <summary>
/// Concesión explícita de acceso de un <see cref="User"/> a un
/// <c>Client</c> concreto. Tener el rol de sistema CLIENTE NO otorga acceso
/// implícito a ningún cliente — el acceso real se resuelve exclusivamente
/// por esta tabla.
/// Tabla física: <c>user_client_access</c> — PK compuesta
/// <c>(user_id, client_id)</c>, tenant-scoped, sin columna <c>id</c>.
/// Colección owned dentro de <see cref="User.ClientAccess"/>, sin
/// <c>DbSet</c> propio.
/// </summary>
public sealed class UserClientAccess
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid? GrantedByUserId { get; private set; }
    public DateTime GrantedAtUtc { get; private set; }

    private UserClientAccess() { }

    public UserClientAccess(Guid tenantId, Guid userId, Guid clientId, Guid? grantedByUserId)
    {
        TenantId = tenantId;
        UserId = userId;
        ClientId = clientId;
        GrantedByUserId = grantedByUserId;
    }
}
