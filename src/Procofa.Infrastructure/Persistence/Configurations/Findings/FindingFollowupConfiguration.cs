using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Findings;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Findings;

/// <summary>Mapeo fiel de <c>public.finding_followups</c>.</summary>
public sealed class FindingFollowupConfiguration : IEntityTypeConfiguration<FindingFollowup>
{
    public void Configure(EntityTypeBuilder<FindingFollowup> builder)
    {
        builder.ToTable("finding_followups");

        builder.HasKey(x => x.Id).HasName("finding_followups_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.FindingId).HasColumnName("finding_id").IsRequired();
        builder.Property(x => x.CorrectiveActionId).HasColumnName("corrective_action_id");
        builder.Property(x => x.AuthorUserId).HasColumnName("author_user_id").IsRequired();

        // varchar(50) SIN CHECK en la BD real — texto libre, no enum.
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();

        builder.Property(x => x.Comment).HasColumnName("comment").HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_finding_followups_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Finding>().WithMany().HasForeignKey(x => x.FindingId).HasConstraintName("fk_finding_followups_finding").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CorrectiveAction>().WithMany().HasForeignKey(x => x.CorrectiveActionId).HasConstraintName("fk_finding_followups_action").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorUserId).HasConstraintName("fk_finding_followups_author").OnDelete(DeleteBehavior.Restrict);
    }
}
