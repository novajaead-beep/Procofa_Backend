using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Entities.Identity;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Checklists;

/// <summary>Mapeo fiel de <c>public.criteria</c>. Sin timestamps — fidelidad física.</summary>
public sealed class CriterionConfiguration : IEntityTypeConfiguration<Criterion>
{
    public void Configure(EntityTypeBuilder<Criterion> builder)
    {
        builder.ToTable("criteria", table =>
        {
            table.HasCheckConstraint(
                "criteria_importance_level_check",
                "importance_level IS NULL OR (importance_level)::text = ANY (ARRAY['ALTA','MEDIA','BAJA']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("criteria_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ChecklistSectionId).HasColumnName("checklist_section_id").IsRequired();

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.AuditQuestion).HasColumnName("audit_question").HasColumnType("text").IsRequired();
        builder.Property(x => x.AuditorInterpretation).HasColumnName("auditor_interpretation").HasColumnType("text");
        builder.Property(x => x.ExpectedEvidence).HasColumnName("expected_evidence").HasColumnType("text");

        // Texto libre sin CHECK en la BD real — NO es el enum EvidenceType. Ver Criterion.cs.
        builder.Property(x => x.ExpectedEvidenceType).HasColumnName("evidence_type").HasMaxLength(80);

        builder.Property(x => x.ImportanceLevel)
            .HasColumnName("importance_level")
            .HasConversion(new ImportanceLevelConverter())
            .HasMaxLength(20);

        builder.Property(x => x.NormativeReference).HasColumnName("normative_reference").HasColumnType("text");
        builder.Property(x => x.EvaluationRecommendation).HasColumnName("evaluation_recommendation").HasColumnType("text");
        builder.Property(x => x.IsMandatory).HasColumnName("is_mandatory").HasDefaultValue(true);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);


        builder.HasIndex(x => new { x.ChecklistSectionId, x.Code })
            .IsUnique()
            .HasDatabaseName("uq_criteria_section_code");

        builder.HasIndex(x => new { x.TenantId, x.ChecklistSectionId, x.SortOrder })
            .HasDatabaseName("ix_criteria_section");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_criteria_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ChecklistSection>().WithMany().HasForeignKey(x => x.ChecklistSectionId).HasConstraintName("fk_criteria_section").OnDelete(DeleteBehavior.Restrict);
    }
}
