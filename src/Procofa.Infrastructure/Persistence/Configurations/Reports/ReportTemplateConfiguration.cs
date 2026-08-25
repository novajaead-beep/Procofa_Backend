using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Reports;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Reports;

/// <summary>
/// Mapeo fiel de <c>public.report_templates</c>. A diferencia de la
/// mayoría de tablas tenant-scoped, su FK a <c>tenants</c> es
/// <c>ON DELETE RESTRICT</c> (no <c>CASCADE</c>) — se replica tal cual
/// existe en la BD real, sin "corregir" el diseño físico.
/// </summary>
public sealed class ReportTemplateConfiguration : IEntityTypeConfiguration<ReportTemplate>
{
    public void Configure(EntityTypeBuilder<ReportTemplate> builder)
    {
        builder.ToTable("report_templates", table =>
        {
            table.HasCheckConstraint(
                "report_templates_report_type_check",
                "(report_type)::text = ANY (ARRAY['FINAL','EJECUTIVO','HALLAZGOS','ACCIONES','SEGUIMIENTO']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("report_templates_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();

        builder.Property(x => x.ReportType)
            .HasColumnName("report_type")
            .HasConversion(new ReportTypeConverter())
            .HasMaxLength(30)
            .IsRequired();


        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_report_templates_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("uq_report_templates_tenant_code");

        builder.HasIndex(x => new { x.TenantId, x.ReportType, x.IsActive })
            .HasDatabaseName("ix_report_templates_active");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_report_templates_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasConstraintName("fk_report_templates_created_by").OnDelete(DeleteBehavior.Restrict);
    }
}
