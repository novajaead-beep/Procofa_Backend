using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Clients;

/// <summary>Mapeo fiel de <c>public.audited_companies</c>.</summary>
public sealed class AuditedCompanyConfiguration : IEntityTypeConfiguration<AuditedCompany>
{
    public void Configure(EntityTypeBuilder<AuditedCompany> builder)
    {
        builder.ToTable("audited_companies");

        builder.HasKey(x => x.Id).HasName("audited_companies_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ClientId).HasColumnName("client_id").IsRequired();
        builder.Property(x => x.DefaultProfileId).HasColumnName("default_profile_id");

        builder.Property(x => x.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TradeName).HasColumnName("trade_name").HasMaxLength(200);
        builder.Property(x => x.TaxId).HasColumnName("tax_id").HasMaxLength(30);
        builder.Property(x => x.Industry).HasColumnName("industry").HasMaxLength(150);
        builder.Property(x => x.CompanyType).HasColumnName("company_type").HasMaxLength(100);

        builder.Property(x => x.IsClientCompany).HasColumnName("is_client_company").HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_audited_companies_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.TenantId, x.ClientId, x.TaxId })
            .IsUnique()
            .HasDatabaseName("uq_audited_company_client_tax_id")
            .HasFilter("tax_id IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.ClientId })
            .HasDatabaseName("ix_audited_companies_client");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_audited_companies_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).HasConstraintName("fk_audited_companies_client").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Profile>().WithMany().HasForeignKey(x => x.DefaultProfileId).HasConstraintName("fk_audited_companies_profile").OnDelete(DeleteBehavior.SetNull);
    }
}
