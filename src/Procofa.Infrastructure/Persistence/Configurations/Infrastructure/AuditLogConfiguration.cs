using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Infrastructure;

namespace Procofa.Infrastructure.Persistence.Configurations.Infrastructure;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        var ipAddressConverter = new ValueConverter<string?, IPAddress>(
            value => IPAddress.Parse(value!),
            value => value.ToString());

        builder.ToTable("audit_logs");

        builder.HasKey(x => x.Id).HasName("audit_logs_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.RoleCode).HasColumnName("role_code").HasMaxLength(30);
        builder.Property(x => x.AuditId).HasColumnName("audit_id");
        builder.Property(x => x.EntityName).HasColumnName("entity_name").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityId).HasColumnName("entity_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(80).IsRequired();

        // jsonb — Domain se mantiene agnóstico de la librería de serialización.
        builder.Property(x => x.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(x => x.NewValues).HasColumnName("new_values").HasColumnType("jsonb");

        builder.Property(x => x.IpAddress)
            .HasColumnName("ip_address")
            .HasConversion(ipAddressConverter)
            .HasColumnType("inet");
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.TenantId, x.AuditId, x.CreatedAtUtc })
            .HasDatabaseName("ix_audit_logs_audit")
            .IsDescending(false, false, true);

        builder.HasIndex(x => new { x.TenantId, x.EntityName, x.EntityId, x.CreatedAtUtc })
            .HasDatabaseName("ix_audit_logs_entity")
            .IsDescending(false, false, false, true);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_audit_logs_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_audit_logs_audit").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_audit_logs_user").OnDelete(DeleteBehavior.SetNull);
    }
}
