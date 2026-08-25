using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Infrastructure;
using Procofa.Domain.Enums;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Infrastructure;

/// <summary>Mapeo fiel de <c>public.notifications</c>.</summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", table =>
        {
            table.HasCheckConstraint(
                "notifications_channel_check",
                "(channel)::text = ANY (ARRAY['INTERNAL','EMAIL']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("notifications_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(x => x.Channel)
            .HasColumnName("channel")
            .HasConversion(new NotificationChannelConverter())
            .HasMaxLength(20)
            .HasDefaultValue(NotificationChannel.Internal)
            .IsRequired();


        builder.Property(x => x.NotificationType).HasColumnName("notification_type").HasMaxLength(50).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(x => x.RelatedEntity).HasColumnName("related_entity").HasMaxLength(50);
        builder.Property(x => x.RelatedEntityId).HasColumnName("related_entity_id");
        builder.Property(x => x.IsRead).HasColumnName("is_read").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.ReadAtUtc).HasColumnName("read_at_utc");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Índice parcial — solo notificaciones no leídas, orden DESC por fecha.
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.CreatedAtUtc })
            .HasDatabaseName("ix_notifications_unread")
            .HasFilter("is_read = false")
            .IsDescending(false, false, true);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_notifications_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_notifications_user").OnDelete(DeleteBehavior.Cascade);
    }
}
