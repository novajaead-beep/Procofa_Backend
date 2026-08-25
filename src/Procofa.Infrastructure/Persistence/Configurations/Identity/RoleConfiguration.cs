using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// Mapeo fiel de <c>public.roles</c> — catálogo global SIN <c>tenant_id</c>,
/// sin RLS. Incluye la colección owned <c>Role.Permissions</c>
/// (tabla física <c>public.role_permissions</c>, PK compuesta
/// <c>(role_id, permission_id)</c>, sin columna <c>id</c>, sin
/// <c>tenant_id</c>).
/// </summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(x => x.Id).HasName("roles_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("roles_code_key");

        builder.OwnsMany(x => x.Permissions, rp =>
        {
            rp.ToTable("role_permissions");
            rp.WithOwner().HasForeignKey(x => x.RoleId).HasConstraintName("fk_role_permissions_role");
            rp.HasKey(x => new { x.RoleId, x.PermissionId }).HasName("pk_role_permissions");

            rp.Property(x => x.RoleId).HasColumnName("role_id");
            rp.Property(x => x.PermissionId).HasColumnName("permission_id");

            rp.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(x => x.PermissionId).HasConstraintName("fk_role_permissions_permission")
                .OnDelete(DeleteBehavior.Cascade);

            rp.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(x => x.Permissions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
