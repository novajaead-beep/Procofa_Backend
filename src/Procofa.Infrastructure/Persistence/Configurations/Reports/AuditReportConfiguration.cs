using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Reports;
using Procofa.Domain.Enums;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Reports;

/// <summary>
/// Mapeo fiel de <c>public.audit_reports</c>. Sin columnas
/// <c>created_at_utc</c>/<c>updated_at_utc</c> — no existen físicamente
/// (ver <see cref="AuditReport"/>). La inmutabilidad de reportes
/// <c>FINAL</c> la impone <c>trg_audit_reports_final_immutable</c> a nivel
/// de BD — replicar ese comportamiento en EF/Application queda fuera de
/// alcance de Instrucción 03.
/// </summary>
public sealed class AuditReportConfiguration : IEntityTypeConfiguration<AuditReport>
{
    public void Configure(EntityTypeBuilder<AuditReport> builder)
    {
        builder.ToTable("audit_reports", table =>
        {
            table.HasCheckConstraint(
                "audit_reports_report_type_check",
                "(report_type)::text = ANY (ARRAY['FINAL','EJECUTIVO','HALLAZGOS','ACCIONES','SEGUIMIENTO']::text[])");
            table.HasCheckConstraint(
                "audit_reports_version_number_check",
                "version_number > 0");
            table.HasCheckConstraint(
                "audit_reports_format_check",
                "(format)::text = ANY (ARRAY['PDF','DOCX','XLSX']::text[])");
            table.HasCheckConstraint(
                "audit_reports_status_check",
                "(status)::text = ANY (ARRAY['DRAFT','FINAL','VOID']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("audit_reports_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditId).HasColumnName("audit_id").IsRequired();
        builder.Property(x => x.ReportTemplateVersionId).HasColumnName("report_template_version_id");

        builder.Property(x => x.ReportType)
            .HasColumnName("report_type")
            .HasConversion(new ReportTypeConverter())
            .HasMaxLength(30)
            .IsRequired();


        builder.Property(x => x.VersionNumber).HasColumnName("version_number").HasDefaultValue(1).IsRequired();

        builder.Property(x => x.Format)
            .HasColumnName("format")
            .HasConversion(new ReportFormatConverter())
            .HasMaxLength(10)
            .IsRequired();


        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(new AuditReportStatusConverter())
            .HasMaxLength(20)
            .HasDefaultValue(AuditReportStatus.Draft)
            .IsRequired();


        builder.Property(x => x.StorageKey).HasColumnName("storage_key").HasColumnType("text").IsRequired();
        builder.Property(x => x.Sha256Hex).HasColumnName("sha256_hex").HasMaxLength(64);
        builder.Property(x => x.GeneratedByUserId).HasColumnName("generated_by_user_id").IsRequired();
        builder.Property(x => x.ValidatedByUserId).HasColumnName("validated_by_user_id");

        builder.Property(x => x.GeneratedAtUtc)
            .HasColumnName("generated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ValidatedAtUtc).HasColumnName("validated_at_utc");

        builder.HasIndex(x => new { x.AuditId, x.ReportType, x.VersionNumber, x.Format })
            .IsUnique()
            .HasDatabaseName("uq_audit_report_version");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_audit_reports_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_audit_reports_audit").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ReportTemplateVersion>().WithMany().HasForeignKey(x => x.ReportTemplateVersionId).HasConstraintName("fk_audit_reports_template_version").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.GeneratedByUserId).HasConstraintName("fk_audit_reports_generated_by").OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ValidatedByUserId).HasConstraintName("fk_audit_reports_validated_by")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
