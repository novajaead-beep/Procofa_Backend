namespace Procofa.Domain.Entities.Identity;

/// <summary>
/// Refresh token de sesión (JWT). Entidad independiente, mismo razonamiento
/// que <see cref="PasswordResetToken"/>. Tabla física: <c>refresh_tokens</c>,
/// tenant-scoped, RLS+FORCE RLS, <c>ON DELETE CASCADE</c> desde <c>users</c>.
/// <see cref="TokenHash"/> es <c>UNIQUE</c> a nivel de BD — nunca se
/// persiste el token crudo, solo su hash.
///
/// Nota: esta entidad NO escribe en <c>access_logs</c> en su refresh/revoke — no existe un
/// <c>event_type</c> válido para esas acciones en el CHECK de esa tabla — así que usa structured
/// logging en su lugar.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private RefreshToken() { }

    public RefreshToken(Guid id, Guid tenantId, Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsExpired(DateTime nowUtc) =>
    ExpiresAtUtc <= nowUtc;

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public bool IsActive(DateTime nowUtc) =>
        !IsRevoked && !IsExpired(nowUtc);

    public void Revoke(DateTime nowUtc)
    {
        if (RevokedAtUtc.HasValue)
        {
            return;
        }

        RevokedAtUtc = nowUtc;
    }
}
