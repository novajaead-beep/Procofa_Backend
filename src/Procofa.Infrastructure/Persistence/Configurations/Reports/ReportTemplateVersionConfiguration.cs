using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Reports;
using Procofa.Domain.Enums;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Reports;

/// <summary>
/// Mapeo fiel de <c>public.report_template_versions</c>. FK a
/// <c>tenants</c> y a <c>report_templates</c> son <c>ON DELETE RESTRICT</c>
/// — versiones publicadas no deben perderse silenciosamente. A diferencia
/// de <c>audit_reports.version_number</c>, esta columna NO tiene
/// <c>DEFAULT</c> en la BD real — debe asignarse explícitamente desde
/// Application.
/// </summary>
public sealed class ReportTemplateVersionConfiguration : IEntityTypeConfiguration<ReportTemplateVersion>
{
    public void Configure(EntityTypeBuilder<ReportTemplateVersion> builder)
    {
        builder.ToTable("report_template_versions", table =>
        {
            table.HasCheckConstraint(
                "report_template_versions_version_number_check",
                "version_number > 0");
            table.HasCheckConstraint(
                "report_template_versions_status_check",
                "(status)::text = ANY (ARRAY['DRAFT','PUBLISHED','RETIRED']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("report_template_versions_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ReportTemplateId).HasColumnName("report_template_id").IsRequired();

        builder.Property(x => x.VersionNumber).HasColumnName("version_number").IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(new ReportTemplateVersionStatusConverter())
            .HasMaxLength(20)
            .HasDefaultValue(ReportTemplateVersionStatus.Draft)
            .IsRequired();


        builder.Property(x => x.TemplateStorageKey).HasColumnName("template_storage_key").HasColumnType("text").IsRequired();

        // jsonb — Domain se mantiene agnóstico de librería de serialización;
        // se expone como string crudo, Infrastructure fija el tipo físico.
        builder.Property(x => x.ConfigurationJson).HasColumnName("configuration_json").HasColumnType("jsonb");

        builder.Property(x => x.ChangeNotes).HasColumnName("change_notes").HasColumnType("text");
        builder.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_report_template_versions_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.ReportTemplateId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uq_report_template_version");

        // version_number DESC — orden físico del índice replicado tal cual.
        builder.HasIndex(x => new { x.TenantId, x.ReportTemplateId, x.VersionNumber })
            .HasDatabaseName("ix_report_template_versions_template")
            .IsDescending(false, false, true);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_report_template_versions_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReportTemplate>().WithMany().HasForeignKey(x => x.ReportTemplateId).HasConstraintName("fk_report_template_versions_template").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasConstraintName("fk_report_template_versions_created_by").OnDelete(DeleteBehavior.Restrict);
    }
}
