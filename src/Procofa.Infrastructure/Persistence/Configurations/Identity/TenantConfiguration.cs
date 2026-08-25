using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Identity;

/// <summary>
/// Mapeo fiel de <c>public.tenants</c> (48 tablas, grupo Identidad/seguridad).
/// RLS/FORCE RLS y la policy <c>tenants_isolation</c> (auto-referencial:
/// <c>id = current_setting('app.tenant_id')</c>) NO se modelan aquí — EF
/// Core no tiene representación nativa de RLS; viven en
/// <c>db/baseline/v2.1/002_security.sql</c> (ver Sección F del reporte).
/// </summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(x => x.Id).HasName("tenants_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(200);

        builder.Property(x => x.TaxId)
            .HasColumnName("tax_id")
            .HasMaxLength(30);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_tenants_updated_at (set_updated_at_utc()) — EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("tenants_slug_key");
    }
}
