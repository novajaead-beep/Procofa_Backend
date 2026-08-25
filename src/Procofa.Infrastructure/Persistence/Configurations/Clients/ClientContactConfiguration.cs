using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Clients;

/// <summary>Mapeo fiel de <c>public.client_contacts</c>.</summary>
public sealed class ClientContactConfiguration : IEntityTypeConfiguration<ClientContact>
{
    public void Configure(EntityTypeBuilder<ClientContact> builder)
    {
        builder.ToTable("client_contacts");

        builder.HasKey(x => x.Id).HasName("client_contacts_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ClientId).HasColumnName("client_id").IsRequired();
        builder.Property(x => x.AuditedCompanyId).HasColumnName("audited_company_id");

        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.JobTitle).HasColumnName("job_title").HasMaxLength(120);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);

        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_client_contacts_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.TenantId, x.ClientId })
            .HasDatabaseName("ix_client_contacts_client");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_client_contacts_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).HasConstraintName("fk_client_contacts_client").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AuditedCompany>().WithMany().HasForeignKey(x => x.AuditedCompanyId).HasConstraintName("fk_client_contacts_company").OnDelete(DeleteBehavior.SetNull);
    }
}
