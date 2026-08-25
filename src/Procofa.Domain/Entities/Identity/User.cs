using Procofa.Domain.Entities.Identity.ValueObjects;

namespace Procofa.Domain.Entities.Identity;

/// <summary>
/// Usuario del sistema (personal PROCOFA o de cliente con acceso al portal).
/// Aggregate Root. Tabla física: <c>users</c>, tenant-scoped, RLS+FORCE RLS.
///
/// Posee <see cref="Roles"/> (tabla <c>user_roles</c>, PK compuesta
/// <c>(user_id, role_id)</c>) y <see cref="ClientAccess"/> (tabla
/// <c>user_client_access</c>, PK compuesta <c>(user_id, client_id)</c>) —
/// ambas sin columna <c>id</c> propia → colecciones owned, sin
/// <c>DbSet</c> propio, siguiendo el mismo patrón que <c>AuditTeam</c>
/// dentro de <c>Audit</c>.
///
/// <see cref="NormalizedEmail"/> es recalculada SIEMPRE por el trigger
/// <c>trg_users_normalize_email</c> (<c>normalize_user_email()</c>:
/// <c>email = BTRIM(email)</c>, <c>normalized_email = UPPER(BTRIM(email))</c>)
/// en cada INSERT/UPDATE de <c>email</c> — EF nunca debe depender del valor
/// que él mismo calculó localmente para lo que persiste; si Application
/// necesita el valor normalizado antes de un roundtrip a BD, debe replicar
/// la misma lógica con <c>CultureInfo.InvariantCulture</c> (evita el problema
/// de la "I turca"), nunca <c>ToUpper()</c> con cultura por defecto.
/// </summary>
public sealed class User
{
    private readonly List<UserRole> _roles = [];
    private readonly List<UserClientAccess> _clientAccess = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool MustChangePassword { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_users_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<UserClientAccess> ClientAccess => _clientAccess.AsReadOnly();

    private User() { }

    public User(
        Guid id,
        Guid tenantId,
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phone)
    {
        Id = id;
        TenantId = tenantId;
        Email = email;
        // Valor inicial en memoria; la BD es la autoridad final vía trigger.
        NormalizedEmail = email.Trim().ToUpperInvariant();
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        IsActive = true;
        MustChangePassword = false;
        FailedLoginAttempts = 0;
    }
}
