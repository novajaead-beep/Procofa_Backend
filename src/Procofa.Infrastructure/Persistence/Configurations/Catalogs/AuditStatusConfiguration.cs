using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Configurations.Catalogs;

/// <summary>Mapeo fiel de <c>public.audit_statuses</c>.</summary>
public sealed class AuditStatusConfiguration : IEntityTypeConfiguration<AuditStatus>
{
    public void Configure(EntityTypeBuilder<AuditStatus> builder)
    {
        builder.ToTable("audit_statuses");

        builder.HasKey(x => x.Id).HasName("audit_statuses_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
        builder.Property(x => x.IsTerminal).HasColumnName("is_terminal").HasDefaultValue(false);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("audit_statuses_code_key");
    }
}
