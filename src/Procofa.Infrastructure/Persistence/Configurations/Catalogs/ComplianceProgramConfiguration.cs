using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Configurations.Catalogs;

/// <summary>
/// Mapeo fiel de <c>public.programs</c>. Nombre de tabla explícito porque
/// el tipo C# se llama <see cref="ComplianceProgram"/>, no <c>Program</c>
/// (ver justificación en la propia entidad).
/// </summary>
public sealed class ComplianceProgramConfiguration : IEntityTypeConfiguration<ComplianceProgram>
{
    public void Configure(EntityTypeBuilder<ComplianceProgram> builder)
    {
        builder.ToTable("programs");

        builder.HasKey(x => x.Id).HasName("programs_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("programs_code_key");
    }
}
