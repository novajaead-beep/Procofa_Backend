using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Findings;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Findings;

/// <summary>
/// Mapeo fiel de <c>public.findings</c>. <see cref="Finding.LockVersion"/> es
/// token de concurrencia optimista — ver <c>ConcurrencyTokenInterceptor</c>.
/// </summary>
public sealed class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.ToTable("findings", table =>
        {
            table.HasCheckConstraint(
                "findings_finding_number_check",
                "finding_number > 0");
            table.HasCheckConstraint(
                "findings_lock_version_check",
                "lock_version > 0");
        });

        builder.HasKey(x => x.Id).HasName("findings_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditId).HasColumnName("audit_id").IsRequired();
        builder.Property(x => x.AuditCriterionId).HasColumnName("audit_criterion_id").IsRequired();

        builder.Property(x => x.FindingNumber).HasColumnName("finding_number").IsRequired();

        builder.Property(x => x.FindingTypeId).HasColumnName("finding_type_id").IsRequired();
        builder.Property(x => x.PriorityId).HasColumnName("priority_id").IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("status_id").IsRequired();

        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").IsRequired();
        builder.Property(x => x.ObservedEvidence).HasColumnName("observed_evidence").HasColumnType("text");
        builder.Property(x => x.RiskImpact).HasColumnName("risk_impact").HasColumnType("text");
        builder.Property(x => x.ViolatedRequirement).HasColumnName("violated_requirement").HasColumnType("text");
        builder.Property(x => x.Recommendation).HasColumnName("recommendation").HasColumnType("text");

        builder.Property(x => x.ResponsibleUserId).HasColumnName("responsible_user_id");
        builder.Property(x => x.ResponsibleContactId).HasColumnName("responsible_contact_id");
        builder.Property(x => x.CommitmentDate).HasColumnName("commitment_date");

        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.ValidatedByUserId).HasColumnName("validated_by_user_id");
        builder.Property(x => x.ValidatedAtUtc).HasColumnName("validated_at_utc");
        builder.Property(x => x.ClosedAtUtc).HasColumnName("closed_at_utc");

        builder.Property(x => x.LockVersion)
            .HasColumnName("lock_version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();


        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_findings_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.AuditId, x.FindingNumber })
            .IsUnique()
            .HasDatabaseName("uq_findings_audit_number");

        builder.HasIndex(x => new { x.TenantId, x.AuditId, x.StatusId })
            .HasDatabaseName("ix_findings_audit_status");

        builder.HasIndex(x => new { x.TenantId, x.CommitmentDate })
            .HasDatabaseName("ix_findings_commitment_date")
            .HasFilter("closed_at_utc IS NULL");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_findings_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_findings_audit").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AuditCriterion>().WithMany().HasForeignKey(x => x.AuditCriterionId).HasConstraintName("fk_findings_criterion").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FindingType>().WithMany().HasForeignKey(x => x.FindingTypeId).HasConstraintName("fk_findings_type").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FindingPriority>().WithMany().HasForeignKey(x => x.PriorityId).HasConstraintName("fk_findings_priority").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FindingStatus>().WithMany().HasForeignKey(x => x.StatusId).HasConstraintName("fk_findings_status").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ClientContact>().WithMany().HasForeignKey(x => x.ResponsibleContactId).HasConstraintName("fk_findings_responsible_contact").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasConstraintName("fk_findings_created_by").OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ResponsibleUserId).HasConstraintName("fk_findings_responsible_user")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ValidatedByUserId).HasConstraintName("fk_findings_validated_by")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
