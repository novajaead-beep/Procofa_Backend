using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Entities.Identity;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Checklists;

/// <summary>Mapeo fiel de <c>public.checklist_versions</c>.</summary>
public sealed class ChecklistVersionConfiguration : IEntityTypeConfiguration<ChecklistVersion>
{
    public void Configure(EntityTypeBuilder<ChecklistVersion> builder)
    {
        builder.ToTable("checklist_versions", table =>
        {
            table.HasCheckConstraint(
                "checklist_versions_status_check",
                "(status)::text = ANY (ARRAY['DRAFT','PUBLISHED','RETIRED']::text[])");
            table.HasCheckConstraint(
                "checklist_versions_version_number_check",
                "version_number > 0");
        });

        builder.HasKey(x => x.Id).HasName("checklist_versions_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ChecklistId).HasColumnName("checklist_id").IsRequired();
        builder.Property(x => x.VersionNumber).HasColumnName("version_number").IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(new ChecklistVersionStatusConverter())
            .HasMaxLength(20)
            .HasDefaultValue(Domain.Enums.ChecklistVersionStatus.Draft)
            .IsRequired();

        // Instrucción 03.1, defecto 3: faltaba replicar este CHECK físico
        // (existía en 15 de las 16 columnas VARCHAR+CHECK del baseline; esta
        // era la única omitida).

        builder.Property(x => x.ChangeNotes).HasColumnName("change_notes").HasColumnType("text");
        builder.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_checklist_versions_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();


        builder.HasIndex(x => new { x.ChecklistId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("uq_checklist_version");

        builder.HasIndex(x => new { x.TenantId, x.ChecklistId, x.Status })
            .HasDatabaseName("ix_checklist_versions_checklist");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_checklist_versions_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Checklist>().WithMany().HasForeignKey(x => x.ChecklistId).HasConstraintName("fk_checklist_versions_checklist").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasConstraintName("fk_checklist_versions_created_by").OnDelete(DeleteBehavior.Restrict);
    }
}
