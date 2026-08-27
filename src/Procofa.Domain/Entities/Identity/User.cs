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
        NormalizedEmail = Normalize(email);
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        IsActive = true;
        MustChangePassword = false;
        FailedLoginAttempts = 0;
    }

    /// <summary>
    /// Réplica en memoria de <c>normalize_user_email()</c>
    /// (<c>UPPER(BTRIM(email))</c>) — <see cref="System.Globalization.CultureInfo.InvariantCulture"/>
    /// vía <see cref="string.ToUpperInvariant()"/>, nunca <c>ToUpper()</c> con
    /// cultura por defecto (evita el problema de la "I turca"). Usado por
    /// Application para buscar un usuario por email sin depender de un
    /// roundtrip a BD, y por el constructor para el valor inicial en memoria
    /// (la BD sigue siendo la autoridad final vía trigger).
    /// </summary>
    public static string Normalize(string email) => email.Trim().ToUpperInvariant();

    /// <summary>Instrucción 04: true si <see cref="LockedUntilUtc"/> sigue vigente respecto de <paramref name="nowUtc"/>.</summary>
    public bool IsLockedOut(DateTime nowUtc) => LockedUntilUtc.HasValue && LockedUntilUtc.Value > nowUtc;

    /// <summary>
    /// Instrucción 04, flujo de login paso 8: incrementa
    /// <see cref="FailedLoginAttempts"/> y aplica lockout
    /// (<see cref="LockedUntilUtc"/> = <paramref name="nowUtc"/> + <paramref name="lockoutDuration"/>)
    /// cuando se alcanza <paramref name="maxFailedAttempts"/>. La política
    /// (umbral/duración) es responsabilidad de configuración — este método
    /// solo aplica los valores ya resueltos, sin conocer de dónde vienen.
    /// </summary>
    public void RegisterFailedLogin(int maxFailedAttempts, TimeSpan lockoutDuration, DateTime nowUtc)
    {
        FailedLoginAttempts += 1;

        if (FailedLoginAttempts >= maxFailedAttempts)
        {
            LockedUntilUtc = nowUtc.Add(lockoutDuration);
        }
    }

    /// <summary>Instrucción 04, flujo de login paso 9: resetea intentos/bloqueo y actualiza el último login.</summary>
    public void RegisterSuccessfulLogin(DateTime nowUtc)
    {
        FailedLoginAttempts = 0;
        LockedUntilUtc = null;
        LastLoginAtUtc = nowUtc;
    }

    /// <summary>
    /// Reemplaza el hash de contraseña — usado por el flujo de login cuando
    /// <c>IPasswordHasher</c> señala <c>SuccessRehashNeeded</c> (el hash
    /// verificó correcto pero fue creado con parámetros desactualizados).
    /// Nunca recibe una contraseña en texto plano: solo el hash ya calculado.
    /// </summary>
    public void ChangePasswordHash(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    /// <summary>
    /// Asigna un rol de sistema al usuario (colección owned <see cref="Roles"/>,
    /// tabla <c>user_roles</c>). Usado por el bootstrap del primer ADMIN y por
    /// la creación de usuarios (Instrucción 05).
    /// </summary>
    public void AddRole(UserRole userRole)
    {
        _roles.Add(userRole);
    }

    /// <summary>Instrucción 05: quita un rol puntual (colección owned <see cref="Roles"/>). No-op si el usuario no lo tiene.</summary>
    public void RemoveRole(Guid roleId)
    {
        _roles.RemoveAll(r => r.RoleId == roleId);
    }

    /// <summary>
    /// Instrucción 05, <c>PUT /api/users/{id}/roles</c>: reemplaza el
    /// conjunto completo de roles. EF detecta la diferencia (altas/bajas en
    /// <c>user_roles</c>) al mutar la colección owned en memoria — el
    /// llamador es responsable de construir <paramref name="newRoles"/> ya
    /// resueltos contra el catálogo real (nunca códigos inventados).
    /// </summary>
    public void ReplaceRoles(IEnumerable<UserRole> newRoles)
    {
        _roles.Clear();
        _roles.AddRange(newRoles);
    }

    /// <summary>
    /// Instrucción 05, sección "CREAR USUARIO": el usuario creado por un
    /// ADMIN siempre debe cambiar su contraseña temporal en el primer login.
    /// Método explícito en vez de un setter público — <see cref="MustChangePassword"/>
    /// nunca se asigna directamente desde fuera del dominio.
    /// </summary>
    public void RequirePasswordChange()
    {
        MustChangePassword = true;
    }

    /// <summary>Instrucción 05, <c>PATCH /api/users/{id}/status</c>: reactiva la cuenta (no borra ni resetea nada más).</summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>Instrucción 05, <c>PATCH /api/users/{id}/status</c>: desactiva la cuenta (soft — nunca hard delete). La regla "un ADMIN no puede desactivarse a sí mismo" es responsabilidad del caso de uso, no de esta entidad.</summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Instrucción 05, sección "REGLAS PARA CLIENTE": concede acceso a un
    /// cliente concreto (colección owned <see cref="ClientAccess"/>). Tener
    /// el rol de sistema CLIENTE no otorga acceso implícito — solo esta
    /// colección lo hace.
    /// </summary>
    public void GrantClientAccess(UserClientAccess access)
    {
        _clientAccess.Add(access);
    }

    /// <summary>
    /// Instrucción 05, <c>PUT /api/users/{id}/client-access</c>: reemplaza el
    /// conjunto completo de accesos a clientes. Mismo mecanismo de detección
    /// de cambios que <see cref="ReplaceRoles"/>.
    /// </summary>
    public void ReplaceClientAccess(IEnumerable<UserClientAccess> newAccess)
    {
        _clientAccess.Clear();
        _clientAccess.AddRange(newAccess);
    }

    /// <summary>
    /// Instrucción 05, sección "REGLAS PARA CLIENTE": "si durante actualización
    /// se quita el rol CLIENTE, eliminar sus registros de user_client_access".
    /// </summary>
    public void ClearClientAccess()
    {
        _clientAccess.Clear();
    }
}
