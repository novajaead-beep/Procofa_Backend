using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Findings;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Findings;

/// <summary>
/// Mapeo fiel de <c>public.corrective_actions</c>.
/// <see cref="CorrectiveAction.LockVersion"/> es token de concurrencia
/// optimista — ver <c>ConcurrencyTokenInterceptor</c>.
/// </summary>
public sealed class CorrectiveActionConfiguration : IEntityTypeConfiguration<CorrectiveAction>
{
    public void Configure(EntityTypeBuilder<CorrectiveAction> builder)
    {
        builder.ToTable("corrective_actions", table =>
        {
            table.HasCheckConstraint(
                "corrective_actions_lock_version_check",
                "lock_version > 0");
            table.HasCheckConstraint(
                "ck_corrective_action_responsible",
                "responsible_user_id IS NOT NULL OR responsible_contact_id IS NOT NULL");
        });

        builder.HasKey(x => x.Id).HasName("corrective_actions_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.FindingId).HasColumnName("finding_id").IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("status_id").IsRequired();

        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text").IsRequired();
        builder.Property(x => x.ResponsibleUserId).HasColumnName("responsible_user_id");
        builder.Property(x => x.ResponsibleContactId).HasColumnName("responsible_contact_id");
        builder.Property(x => x.CommitmentDate).HasColumnName("commitment_date").IsRequired();
        builder.Property(x => x.CompletionNotes).HasColumnName("completion_notes").HasColumnType("text");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(x => x.ValidatedByUserId).HasColumnName("validated_by_user_id");
        builder.Property(x => x.ValidatedAtUtc).HasColumnName("validated_at_utc");
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder.Property(x => x.LockVersion)
            .HasColumnName("lock_version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken()
            .IsRequired();


        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_corrective_actions_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.TenantId, x.FindingId, x.StatusId })
            .HasDatabaseName("ix_corrective_actions_finding");

        builder.HasIndex(x => new { x.TenantId, x.CommitmentDate })
            .HasDatabaseName("ix_corrective_actions_commitment_date")
            .HasFilter("completed_at_utc IS NULL");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_corrective_actions_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<CorrectiveActionStatus>().WithMany().HasForeignKey(x => x.StatusId).HasConstraintName("fk_corrective_actions_status").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ClientContact>().WithMany().HasForeignKey(x => x.ResponsibleContactId).HasConstraintName("fk_corrective_actions_responsible_contact").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasConstraintName("fk_corrective_actions_created_by").OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ResponsibleUserId).HasConstraintName("fk_corrective_actions_responsible_user")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ValidatedByUserId).HasConstraintName("fk_corrective_actions_validated_by")
            .OnDelete(DeleteBehavior.SetNull);

        // FK a Finding se configura en FindingConfiguration del lado inverso? No —
        // EF requiere declarar el HasOne<T> aquí: Finding no expone navegación de
        // colección (patrón "solo ID"), así que se declara explícitamente:
        builder.HasOne<Domain.Entities.Findings.Finding>()
            .WithMany()
            .HasForeignKey(x => x.FindingId).HasConstraintName("fk_corrective_actions_finding")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
