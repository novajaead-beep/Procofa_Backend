using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// Mapeo fiel de <c>public.users</c>. Incluye las colecciones owned
/// <c>User.Roles</c> (tabla <c>user_roles</c>) y
/// <c>User.ClientAccess</c> (tabla <c>user_client_access</c>) — ambas PK
/// compuesta, tenant-scoped, sin columna <c>id</c>.
///
/// <see cref="User.NormalizedEmail"/> es recalculada por el trigger
/// <c>trg_users_normalize_email</c> en cada INSERT/UPDATE de
/// <see cref="User.Email"/> — <c>ValueGeneratedOnAddOrUpdate()</c>, EF nunca
/// la escribe explícitamente.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint(
                "users_failed_login_attempts_check",
                "failed_login_attempts >= 0");
        });

        builder.HasKey(x => x.Id).HasName("users_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        // Trigger normalize_user_email(): UPPER(BTRIM(email)). EF nunca la escribe.
        builder.Property(x => x.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(255)
            .IsRequired()
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Phone)
            .HasColumnName("phone")
            .HasMaxLength(30);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.MustChangePassword)
            .HasColumnName("must_change_password")
            .HasDefaultValue(false);

        builder.Property(x => x.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .HasDefaultValue(0);

        builder.Property(x => x.LockedUntilUtc)
            .HasColumnName("locked_until_utc");

        builder.Property(x => x.LastLoginAtUtc)
            .HasColumnName("last_login_at_utc");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_users_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();


        builder.HasIndex(x => new { x.TenantId, x.NormalizedEmail })
            .IsUnique()
            .HasDatabaseName("uq_users_tenant_normalized_email");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_users_tenant");

        builder.HasIndex(x => new { x.TenantId, x.IsActive })
            .HasDatabaseName("ix_users_tenant_active");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId).HasConstraintName("fk_users_tenant")
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(x => x.Roles, ur =>
        {
            ur.ToTable("user_roles");
            ur.WithOwner().HasForeignKey(x => x.UserId).HasConstraintName("fk_user_roles_user");
            ur.HasKey(x => new { x.UserId, x.RoleId }).HasName("pk_user_roles");

            ur.Property(x => x.TenantId).HasColumnName("tenant_id");
            ur.Property(x => x.UserId).HasColumnName("user_id");
            ur.Property(x => x.RoleId).HasColumnName("role_id");
            ur.Property(x => x.AssignedByUserId).HasColumnName("assigned_by_user_id");
            ur.Property(x => x.AssignedAtUtc)
                .HasColumnName("assigned_at_utc")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

            // Instrucción 03.1, defecto 2: FK a tenants faltante.
            ur.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId).HasConstraintName("fk_user_roles_tenant")
                .OnDelete(DeleteBehavior.Cascade);

            ur.HasOne<Role>()
                .WithMany()
                .HasForeignKey(x => x.RoleId).HasConstraintName("fk_user_roles_role")
                .OnDelete(DeleteBehavior.Restrict);

            ur.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AssignedByUserId).HasConstraintName("fk_user_roles_assigned_by")
                .OnDelete(DeleteBehavior.SetNull);

            ur.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.OwnsMany(x => x.ClientAccess, uca =>
        {
            uca.ToTable("user_client_access");
            uca.WithOwner().HasForeignKey(x => x.UserId).HasConstraintName("fk_user_client_access_user");
            uca.HasKey(x => new { x.UserId, x.ClientId }).HasName("pk_user_client_access");

            uca.Property(x => x.TenantId).HasColumnName("tenant_id");
            uca.Property(x => x.UserId).HasColumnName("user_id");
            uca.Property(x => x.ClientId).HasColumnName("client_id");
            uca.Property(x => x.GrantedByUserId).HasColumnName("granted_by_user_id");
            uca.Property(x => x.GrantedAtUtc)
                .HasColumnName("granted_at_utc")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

            // Instrucción 03.1, defecto 2: FK a tenants faltante.
            uca.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId).HasConstraintName("fk_user_client_access_tenant")
                .OnDelete(DeleteBehavior.Cascade);

            uca.HasOne<Client>()
                .WithMany()
                .HasForeignKey(x => x.ClientId).HasConstraintName("fk_user_client_access_client")
                .OnDelete(DeleteBehavior.Cascade);

            uca.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.GrantedByUserId).HasConstraintName("fk_user_client_access_granted_by")
                .OnDelete(DeleteBehavior.SetNull);

            uca.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(x => x.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.ClientAccess).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
