using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Reports;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Reports;

/// <summary>
/// Mapeo fiel de <c>public.audit_signatories</c>. Pertenece a
/// <see cref="Audit"/> directamente (no a <see cref="AuditReport"/>) — ver
/// nota en la entidad. <c>ck_audit_signatory_source</c> se replica tal cual
/// existe en la BD real, aunque en la práctica sea redundante con
/// <c>signer_name NOT NULL</c> (no se "corrige" el diseño físico).
/// </summary>
public sealed class AuditSignatoryConfiguration : IEntityTypeConfiguration<AuditSignatory>
{
    public void Configure(EntityTypeBuilder<AuditSignatory> builder)
    {
        builder.ToTable("audit_signatories", table =>
        {
            table.HasCheckConstraint(
                "audit_signatories_signer_type_check",
                "(signer_type)::text = ANY (ARRAY['AUDITOR_LIDER','AUDITOR','CLIENTE','RESPONSABLE']::text[])");
            table.HasCheckConstraint(
                "ck_audit_signatory_source",
                "user_id IS NOT NULL OR client_contact_id IS NOT NULL OR signer_name IS NOT NULL");
        });

        builder.HasKey(x => x.Id).HasName("audit_signatories_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditId).HasColumnName("audit_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.ClientContactId).HasColumnName("client_contact_id");

        builder.Property(x => x.SignerName).HasColumnName("signer_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.SignerRole).HasColumnName("signer_role").HasMaxLength(150);

        builder.Property(x => x.SignerType)
            .HasColumnName("signer_type")
            .HasConversion(new SignerTypeConverter())
            .HasMaxLength(30)
            .IsRequired();



        builder.Property(x => x.SignatureStorageKey).HasColumnName("signature_storage_key").HasColumnType("text");
        builder.Property(x => x.SignedAtUtc).HasColumnName("signed_at_utc");
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_audit_signatories_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.TenantId, x.AuditId, x.SortOrder })
            .HasDatabaseName("ix_audit_signatories_audit");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_audit_signatories_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_audit_signatories_audit").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ClientContact>().WithMany().HasForeignKey(x => x.ClientContactId).HasConstraintName("fk_audit_signatories_contact").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_audit_signatories_user").OnDelete(DeleteBehavior.SetNull);
    }
}
