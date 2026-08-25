using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// Mapeo fiel de <c>public.password_reset_tokens</c>. Fidelidad: SIN
/// <c>UNIQUE</c> en <c>token_hash</c> (a diferencia de
/// <see cref="RefreshTokenConfiguration"/>) — no se agrega una unicidad que
/// no existe físicamente (baseline V2.1, hallazgo 🟢 sección C, no aplicado
/// por decisión explícita de fidelidad, no por omisión).
/// </summary>
public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(x => x.Id).HasName("password_reset_tokens_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(x => x.UsedAtUtc).HasColumnName("used_at_utc");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_password_reset_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_password_reset_user").OnDelete(DeleteBehavior.Cascade);
    }
}
