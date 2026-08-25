using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Reports;

namespace Procofa.Infrastructure.Persistence.Configurations.Reports;

/// <summary>
/// Mapeo fiel de <c>public.audit_results</c>. Relación 1:1 con
/// <see cref="Audit"/> garantizada por <c>audit_results_audit_id_key
/// UNIQUE(audit_id)</c> — EF no impone 1:1 vía FK simple, se replica aquí
/// como índice único explícito.
/// </summary>
public sealed class AuditResultConfiguration : IEntityTypeConfiguration<AuditResult>
{
    public void Configure(EntityTypeBuilder<AuditResult> builder)
    {
        builder.ToTable("audit_results", table =>
        {
            table.HasCheckConstraint(
                "audit_results_compliance_percentage_check",
                "compliance_percentage IS NULL OR (compliance_percentage >= 0 AND compliance_percentage <= 100)");
            table.HasCheckConstraint(
                "audit_results_evaluated_criteria_count_check",
                "evaluated_criteria_count >= 0");
            table.HasCheckConstraint(
                "audit_results_compliant_criteria_count_check",
                "compliant_criteria_count >= 0");
            table.HasCheckConstraint(
                "audit_results_partially_compliant_criteria_count_check",
                "partially_compliant_criteria_count >= 0");
            table.HasCheckConstraint(
                "audit_results_non_compliant_criteria_count_check",
                "non_compliant_criteria_count >= 0");
            table.HasCheckConstraint(
                "audit_results_not_applicable_criteria_count_check",
                "not_applicable_criteria_count >= 0");
        });

        builder.HasKey(x => x.Id).HasName("audit_results_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditId).HasColumnName("audit_id").IsRequired();

        builder.Property(x => x.ExecutiveSummary).HasColumnName("executive_summary").HasColumnType("text");
        builder.Property(x => x.GeneralResult).HasColumnName("general_result").HasColumnType("text");
        builder.Property(x => x.Conclusions).HasColumnName("conclusions").HasColumnType("text");
        builder.Property(x => x.GeneralRecommendations).HasColumnName("general_recommendations").HasColumnType("text");

        builder.Property(x => x.CompliancePercentage)
            .HasColumnName("compliance_percentage")
            .HasPrecision(5, 2);


        builder.Property(x => x.EvaluatedCriteriaCount).HasColumnName("evaluated_criteria_count").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.CompliantCriteriaCount).HasColumnName("compliant_criteria_count").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.PartiallyCompliantCriteriaCount).HasColumnName("partially_compliant_criteria_count").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.NonCompliantCriteriaCount).HasColumnName("non_compliant_criteria_count").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.NotApplicableCriteriaCount).HasColumnName("not_applicable_criteria_count").HasDefaultValue(0).IsRequired();


        builder.Property(x => x.FinalizedByUserId).HasColumnName("finalized_by_user_id");
        builder.Property(x => x.FinalizedAtUtc).HasColumnName("finalized_at_utc");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_audit_results_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        // Invariante 1:1 con Audit (constraint física: audit_results_audit_id_key).
        builder.HasIndex(x => x.AuditId)
            .IsUnique()
            .HasDatabaseName("audit_results_audit_id_key");

        // Índice adicional replicado tal cual existe en la BD real, aunque
        // sea redundante con el índice único anterior (no se "optimiza").
        builder.HasIndex(x => new { x.TenantId, x.AuditId })
            .HasDatabaseName("ix_audit_results_audit");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_audit_results_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_audit_results_audit").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.FinalizedByUserId).HasConstraintName("fk_audit_results_finalized_by").OnDelete(DeleteBehavior.SetNull);
    }
}
