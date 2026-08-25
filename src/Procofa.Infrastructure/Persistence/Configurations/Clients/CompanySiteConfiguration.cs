using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Clients;

/// <summary>Mapeo fiel de <c>public.company_sites</c>.</summary>
public sealed class CompanySiteConfiguration : IEntityTypeConfiguration<CompanySite>
{
    public void Configure(EntityTypeBuilder<CompanySite> builder)
    {
        builder.ToTable("company_sites");

        builder.HasKey(x => x.Id).HasName("company_sites_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditedCompanyId).HasColumnName("audited_company_id").IsRequired();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(x => x.AddressLine1).HasColumnName("address_line1").HasMaxLength(200).IsRequired();
        builder.Property(x => x.AddressLine2).HasColumnName("address_line2").HasMaxLength(200);
        builder.Property(x => x.City).HasColumnName("city").HasMaxLength(120);
        builder.Property(x => x.StateRegion).HasColumnName("state_region").HasMaxLength(120);
        builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20);

        builder.Property(x => x.Country)
            .HasColumnName("country")
            .HasMaxLength(100)
            .HasDefaultValue("México")
            .IsRequired();

        builder.Property(x => x.Latitude).HasColumnName("latitude").HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasColumnName("longitude").HasPrecision(9, 6);

        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_company_sites_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.TenantId, x.AuditedCompanyId })
            .HasDatabaseName("ix_company_sites_company");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_company_sites_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AuditedCompany>().WithMany().HasForeignKey(x => x.AuditedCompanyId).HasConstraintName("fk_company_sites_company").OnDelete(DeleteBehavior.Cascade);
    }
}
