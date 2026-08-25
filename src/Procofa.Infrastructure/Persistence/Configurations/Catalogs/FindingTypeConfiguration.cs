using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Configurations.Catalogs;

/// <summary>Mapeo fiel de <c>public.finding_types</c>. Sin <c>is_active</c> — fidelidad física.</summary>
public sealed class FindingTypeConfiguration : IEntityTypeConfiguration<FindingType>
{
    public void Configure(EntityTypeBuilder<FindingType> builder)
    {
        builder.ToTable("finding_types");

        builder.HasKey(x => x.Id).HasName("finding_types_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("finding_types_code_key");
    }
}
