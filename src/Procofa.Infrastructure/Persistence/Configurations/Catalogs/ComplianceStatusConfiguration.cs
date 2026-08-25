using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Configurations.Catalogs;

/// <summary>Mapeo fiel de <c>public.compliance_statuses</c>.</summary>
public sealed class ComplianceStatusConfiguration : IEntityTypeConfiguration<ComplianceStatus>
{
    public void Configure(EntityTypeBuilder<ComplianceStatus> builder)
    {
        builder.ToTable("compliance_statuses");

        builder.HasKey(x => x.Id).HasName("compliance_statuses_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ScoreWeight).HasColumnName("score_weight").HasPrecision(5, 2);
        builder.Property(x => x.IncludedInScore).HasColumnName("included_in_score").HasDefaultValue(true);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("compliance_statuses_code_key");
    }
}
