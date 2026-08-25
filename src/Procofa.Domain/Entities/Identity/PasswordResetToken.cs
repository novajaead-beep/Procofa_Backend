namespace Procofa.Domain.Entities.Identity;

/// <summary>
/// Token de restablecimiento de contraseña. Entidad independiente (no owned
/// por <c>User</c>): se crea/consulta/expira a alta frecuencia por su propio
/// <c>token_hash</c>, sin necesidad de cargar el aggregate <c>User</c> completo.
/// Tabla física: <c>password_reset_tokens</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>users</c>.
///
/// Nota de fidelidad (baseline V2.1, hallazgo 🟢 sección C): a diferencia de
/// <see cref="RefreshToken.TokenHash"/>, <c>token_hash</c> aquí NO tiene
/// <c>UNIQUE</c> en la BD real. Se mapea fielmente sin agregar una unicidad
/// que no existe físicamente — agregarla en EF crearía una divergencia
/// modelo↔BD que Fase B del InitialBaseline marcaría como diferencia real.
/// </summary>
public sealed class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private PasswordResetToken() { }

    public PasswordResetToken(Guid id, Guid tenantId, Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }
}
