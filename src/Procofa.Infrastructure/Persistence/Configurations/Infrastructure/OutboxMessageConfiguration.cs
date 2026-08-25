using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Infrastructure;
using Procofa.Domain.Enums;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Infrastructure;

/// <summary>
/// Mapeo fiel de <c>public.outbox_messages</c>. Índice parcial
/// <c>ix_outbox_pending</c> soporta el futuro <c>BackgroundService</c> de
/// despacho (fuera de alcance de Instrucción 03). Sin tenant-trigger de
/// <c>enforce_same_tenant_references</c> — no aplica, esta tabla no
/// referencia otras entidades tenant-scoped además de <c>tenants</c>.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", table =>
        {
            table.HasCheckConstraint(
                "outbox_messages_status_check",
                "(status)::text = ANY (ARRAY['PENDING','PROCESSING','PROCESSED','FAILED']::text[])");
            table.HasCheckConstraint(
                "outbox_messages_attempts_check",
                "attempts >= 0");
        });

        builder.HasKey(x => x.Id).HasName("outbox_messages_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(120).IsRequired();
        builder.Property(x => x.AggregateType).HasColumnName("aggregate_type").HasMaxLength(80);
        builder.Property(x => x.AggregateId).HasColumnName("aggregate_id");

        // jsonb NOT NULL — Domain se mantiene agnóstico de la librería de serialización.
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(new OutboxMessageStatusConverter())
            .HasMaxLength(20)
            .HasDefaultValue(OutboxMessageStatus.Pending)
            .IsRequired();


        builder.Property(x => x.Attempts).HasColumnName("attempts").HasDefaultValue(0).IsRequired();

        builder.Property(x => x.AvailableAtUtc)
            .HasColumnName("available_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");
        builder.Property(x => x.LastError).HasColumnName("last_error").HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Índice parcial — solo mensajes pendientes de despacho o fallidos.
        builder.HasIndex(x => new { x.TenantId, x.Status, x.AvailableAtUtc })
            .HasDatabaseName("ix_outbox_pending")
            .HasFilter("(status)::text = ANY (ARRAY['PENDING','FAILED']::text[])");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_outbox_tenant").OnDelete(DeleteBehavior.Cascade);
    }
}
