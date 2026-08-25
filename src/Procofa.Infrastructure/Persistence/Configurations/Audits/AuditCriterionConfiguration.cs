using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Audits;

/// <summary>
/// Mapeo fiel de <c>public.audit_criteria</c>. <see cref="AuditCriterion.LockVersion"/>
/// es token de concurrencia optimista (<c>.IsConcurrencyToken()</c>) — el
/// incremento en cada UPDATE real es responsabilidad de
/// <c>ConcurrencyTokenInterceptor</c> (Infrastructure), no de un trigger SQL.
/// </summary>
public sealed class AuditCriterionConfiguration : IEntityTypeConfiguration<AuditCriterion>
{
    public void Configure(EntityTypeBuilder<AuditCriterion> builder)
    {
        builder.ToTable("audit_criteria", table =>
        {
            table.HasCheckConstraint(
                "audit_criteria_lock_version_check",
                "lock_version > 0");
        });

        builder.HasKey(x => x.Id).HasName("audit_criteria_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditId).HasColumnName("audit_id").IsRequired();
        builder.Property(x => x.AuditChecklistId).HasColumnName("audit_checklist_id").IsRequired();
        builder.Property(x => x.CriterionId).HasColumnName("criterion_id").IsRequired();
        builder.Property(x => x.ComplianceStatusId).HasColumnName("compliance_status_id");

        builder.Property(x => x.CriterionCodeSnapshot).HasColumnName("criterion_code_snapshot").HasMaxLength(80).IsRequired();
        builder.Property(x => x.QuestionSnapshot).HasColumnName("question_snapshot").HasColumnType("text").IsRequired();
        builder.Property(x => x.NormativeReferenceSnapshot).HasColumnName("normative_reference_snapshot").HasColumnType("text");
        builder.Property(x => x.IsMandatorySnapshot).HasColumnName("is_mandatory_snapshot").IsRequired();

        builder.Property(x => x.AuditedResponse).HasColumnName("audited_response").HasColumnType("text");
        builder.Property(x => x.IdentifiedRisk).HasColumnName("identified_risk").HasColumnType("text");
        builder.Property(x => x.Recommendation).HasColumnName("recommendation").HasColumnType("text");

        builder.Property(x => x.EvaluatedByUserId).HasColumnName("evaluated_by_user_id");
        builder.Property(x => x.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");

        builder.Property(x => x.LockVersion)
            .HasColumnName("lock_version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();


        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_audit_criteria_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.AuditId, x.CriterionId })
            .IsUnique()
            .HasDatabaseName("uq_audit_criterion");

        builder.HasIndex(x => new { x.TenantId, x.AuditId })
            .HasDatabaseName("ix_audit_criteria_audit");

        builder.HasIndex(x => new { x.TenantId, x.AuditId })
            .HasDatabaseName("ix_audit_criteria_pending")
            .HasFilter("compliance_status_id IS NULL");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_audit_criteria_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_audit_criteria_audit").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AuditChecklist>().WithMany().HasForeignKey(x => x.AuditChecklistId).HasConstraintName("fk_audit_criteria_checklist").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Criterion>().WithMany().HasForeignKey(x => x.CriterionId).HasConstraintName("fk_audit_criteria_criterion").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ComplianceStatus>().WithMany().HasForeignKey(x => x.ComplianceStatusId).HasConstraintName("fk_audit_criteria_compliance").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.EvaluatedByUserId).HasConstraintName("fk_audit_criteria_evaluated_by").OnDelete(DeleteBehavior.SetNull);
    }
}
