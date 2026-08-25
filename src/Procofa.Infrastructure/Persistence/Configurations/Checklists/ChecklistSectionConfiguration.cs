using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Checklists;

/// <summary>Mapeo fiel de <c>public.checklist_sections</c>. Sin timestamps — fidelidad física.</summary>
public sealed class ChecklistSectionConfiguration : IEntityTypeConfiguration<ChecklistSection>
{
    public void Configure(EntityTypeBuilder<ChecklistSection> builder)
    {
        builder.ToTable("checklist_sections");

        builder.HasKey(x => x.Id).HasName("checklist_sections_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.ChecklistVersionId).HasColumnName("checklist_version_id").IsRequired();

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_checklist_sections_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ChecklistVersion>().WithMany().HasForeignKey(x => x.ChecklistVersionId).HasConstraintName("fk_checklist_sections_version").OnDelete(DeleteBehavior.Restrict);
    }
}
