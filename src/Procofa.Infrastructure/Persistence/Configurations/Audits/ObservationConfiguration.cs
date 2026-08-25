using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Identity;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Audits;

/// <summary>Mapeo fiel de <c>public.observations</c>.</summary>
public sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("observations", table =>
        {
            table.HasCheckConstraint(
                "observations_observation_type_check",
                "(observation_type)::text = ANY (ARRAY['AUDITOR','CLIENTE','INTERNA']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("observations_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditId).HasColumnName("audit_id").IsRequired();
        builder.Property(x => x.AuditCriterionId).HasColumnName("audit_criterion_id").IsRequired();
        builder.Property(x => x.AuthorUserId).HasColumnName("author_user_id").IsRequired();

        builder.Property(x => x.ObservationType)
            .HasColumnName("observation_type")
            .HasConversion(new ObservationTypeConverter())
            .HasMaxLength(30)
            .HasDefaultValue(Domain.Enums.ObservationType.Auditor)
            .IsRequired();


        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.TenantId, x.AuditCriterionId, x.CreatedAtUtc })
            .HasDatabaseName("ix_observations_criterion");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_observations_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_observations_audit").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AuditCriterion>().WithMany().HasForeignKey(x => x.AuditCriterionId).HasConstraintName("fk_observations_criterion").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorUserId).HasConstraintName("fk_observations_author").OnDelete(DeleteBehavior.Restrict);
    }
}
