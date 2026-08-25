using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Findings;
using Procofa.Domain.Entities.Identity;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Findings;

/// <summary>Mapeo fiel de <c>public.audit_evidences</c>.</summary>
public sealed class AuditEvidenceConfiguration : IEntityTypeConfiguration<AuditEvidence>
{
    public void Configure(EntityTypeBuilder<AuditEvidence> builder)
    {
        builder.ToTable("audit_evidences", table =>
        {
            table.HasCheckConstraint(
                "audit_evidences_evidence_type_check",
                "(evidence_type)::text = ANY (ARRAY['FOTO','PDF','WORD','EXCEL','IMAGEN','CAPTURA','REGISTRO','OTRO']::text[])");
            table.HasCheckConstraint(
                "audit_evidences_annex_order_check",
                "annex_order IS NULL OR annex_order > 0");
            table.HasCheckConstraint(
                "audit_evidences_file_size_bytes_check",
                "file_size_bytes IS NULL OR file_size_bytes >= 0");
        });

        builder.HasKey(x => x.Id).HasName("audit_evidences_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditId).HasColumnName("audit_id").IsRequired();
        builder.Property(x => x.AuditCriterionId).HasColumnName("audit_criterion_id");
        builder.Property(x => x.FindingId).HasColumnName("finding_id");
        builder.Property(x => x.CorrectiveActionId).HasColumnName("corrective_action_id");
        builder.Property(x => x.DocumentRequestId).HasColumnName("document_request_id");
        builder.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id").IsRequired();

        builder.Property(x => x.EvidenceType)
            .HasColumnName("evidence_type")
            .HasConversion(new EvidenceTypeConverter())
            .HasMaxLength(30)
            .IsRequired();


        builder.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasColumnType("text").IsRequired();
        builder.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(150);
        builder.Property(x => x.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(x => x.Sha256Hex).HasColumnName("sha256_hex").HasMaxLength(64);
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");

        builder.Property(x => x.IsReportRelevant).HasColumnName("is_report_relevant").HasDefaultValue(true);
        builder.Property(x => x.IncludeInReport).HasColumnName("include_in_report").HasDefaultValue(true);
        builder.Property(x => x.IncludeAsAnnex).HasColumnName("include_as_annex").HasDefaultValue(false);
        builder.Property(x => x.AnnexOrder).HasColumnName("annex_order");
        builder.Property(x => x.Caption).HasColumnName("caption").HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();


        builder.HasIndex(x => new { x.TenantId, x.AuditId, x.CreatedAtUtc })
            .HasDatabaseName("ix_evidences_audit");

        builder.HasIndex(x => new { x.TenantId, x.AuditCriterionId })
            .HasDatabaseName("ix_evidences_criterion")
            .HasFilter("audit_criterion_id IS NOT NULL");

        builder.HasIndex(x => new { x.TenantId, x.FindingId })
            .HasDatabaseName("ix_evidences_finding")
            .HasFilter("finding_id IS NOT NULL");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_evidence_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_evidence_audit").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AuditCriterion>().WithMany().HasForeignKey(x => x.AuditCriterionId).HasConstraintName("fk_evidence_criterion").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Finding>().WithMany().HasForeignKey(x => x.FindingId).HasConstraintName("fk_evidence_finding").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<CorrectiveAction>().WithMany().HasForeignKey(x => x.CorrectiveActionId).HasConstraintName("fk_evidence_corrective_action").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<AuditDocumentRequest>().WithMany().HasForeignKey(x => x.DocumentRequestId).HasConstraintName("fk_evidence_document_request").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedByUserId).HasConstraintName("fk_evidence_uploader").OnDelete(DeleteBehavior.Restrict);
    }
}
