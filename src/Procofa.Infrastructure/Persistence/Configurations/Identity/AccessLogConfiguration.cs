using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Entities.Identity;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Identity;

public sealed class AccessLogConfiguration : IEntityTypeConfiguration<AccessLog>
{
    public void Configure(EntityTypeBuilder<AccessLog> builder)
    {
        var ipAddressConverter = new ValueConverter<string?, IPAddress>(
            value => IPAddress.Parse(value!),
            value => value.ToString());

        builder.ToTable("access_logs", table =>
        {
            table.HasCheckConstraint(
                "access_logs_event_type_check",
                "(event_type)::text = ANY (ARRAY['LOGIN_SUCCESS','LOGIN_FAILURE','LOGOUT','PASSWORD_RESET_REQUEST','PASSWORD_RESET_SUCCESS','ACCOUNT_LOCKED']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("access_logs_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");

        builder.Property(x => x.AttemptedEmail)
            .HasColumnName("attempted_email")
            .HasMaxLength(255);

        builder.Property(x => x.EventType)
            .HasColumnName("event_type")
            .HasConversion(new AccessLogEventTypeConverter())
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasConversion(ipAddressConverter)
            .HasColumnType("inet");

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();


        builder.HasIndex(x => new { x.TenantId, x.UserId, x.CreatedAtUtc })
            .HasDatabaseName("ix_access_logs_user")
            .IsDescending(false, false, true);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_access_logs_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_access_logs_user").OnDelete(DeleteBehavior.SetNull);
    }
}
