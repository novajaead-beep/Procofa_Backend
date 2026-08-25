using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Audits;

/// <summary>Mapeo fiel de <c>public.audit_checklists</c>.</summary>
public sealed class AuditChecklistConfiguration : IEntityTypeConfiguration<AuditChecklist>
{
    public void Configure(EntityTypeBuilder<AuditChecklist> builder)
    {
        builder.ToTable("audit_checklists");

        builder.HasKey(x => x.Id).HasName("audit_checklists_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditId).HasColumnName("audit_id").IsRequired();
        builder.Property(x => x.ChecklistVersionId).HasColumnName("checklist_version_id").IsRequired();

        builder.Property(x => x.AssignedAtUtc)
            .HasColumnName("assigned_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.AuditId, x.ChecklistVersionId })
            .IsUnique()
            .HasDatabaseName("uq_audit_checklist_version");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_audit_checklists_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_audit_checklists_audit").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ChecklistVersion>().WithMany().HasForeignKey(x => x.ChecklistVersionId).HasConstraintName("fk_audit_checklists_version").OnDelete(DeleteBehavior.Restrict);
    }
}
