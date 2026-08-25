using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Configurations.Catalogs;

/// <summary>Mapeo fiel de <c>public.finding_statuses</c>.</summary>
public sealed class FindingStatusConfiguration : IEntityTypeConfiguration<FindingStatus>
{
    public void Configure(EntityTypeBuilder<FindingStatus> builder)
    {
        builder.ToTable("finding_statuses");

        builder.HasKey(x => x.Id).HasName("finding_statuses_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsClosed).HasColumnName("is_closed").HasDefaultValue(false);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("finding_statuses_code_key");
    }
}
