using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Configurations.Catalogs;

/// <summary>Mapeo fiel de <c>public.finding_priorities</c>. Sin <c>description</c>/<c>is_active</c> — fidelidad física.</summary>
public sealed class FindingPriorityConfiguration : IEntityTypeConfiguration<FindingPriority>
{
    public void Configure(EntityTypeBuilder<FindingPriority> builder)
    {
        builder.ToTable("finding_priorities");

        builder.HasKey(x => x.Id).HasName("finding_priorities_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(60).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("finding_priorities_code_key");
    }
}
