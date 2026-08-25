using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Checklists;

/// <summary>Mapeo fiel de <c>public.checklists</c>.</summary>
public sealed class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.ToTable("checklists");

        builder.HasKey(x => x.Id).HasName("checklists_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ProgramId).HasColumnName("program_id").IsRequired();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.AuditTypeId).HasColumnName("audit_type_id");

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_checklists_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.TenantId, x.ProgramId, x.ProfileId, x.AuditTypeId, x.IsActive })
            .HasDatabaseName("ix_checklists_selector");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_checklists_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ComplianceProgram>().WithMany().HasForeignKey(x => x.ProgramId).HasConstraintName("fk_checklists_program").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Profile>().WithMany().HasForeignKey(x => x.ProfileId).HasConstraintName("fk_checklists_profile").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AuditType>().WithMany().HasForeignKey(x => x.AuditTypeId).HasConstraintName("fk_checklists_audit_type").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasConstraintName("fk_checklists_created_by").OnDelete(DeleteBehavior.Restrict);
    }
}
