using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Configurations.Catalogs;

/// <summary>Mapeo fiel de <c>public.corrective_action_statuses</c>.</summary>
public sealed class CorrectiveActionStatusConfiguration : IEntityTypeConfiguration<CorrectiveActionStatus>
{
    public void Configure(EntityTypeBuilder<CorrectiveActionStatus> builder)
    {
        builder.ToTable("corrective_action_statuses");

        builder.HasKey(x => x.Id).HasName("corrective_action_statuses_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsClosed).HasColumnName("is_closed").HasDefaultValue(false);
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("corrective_action_statuses_code_key");
    }
}
