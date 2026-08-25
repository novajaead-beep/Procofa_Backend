using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Infrastructure;

namespace Procofa.Infrastructure.Persistence.Configurations.Infrastructure;

/// <summary>
/// Mapeo fiel de <c>public.audit_logs</c>. Tabla append-only — sin
/// <c>updated_at_utc</c> (no existe físicamente); UPDATE/DELETE están
/// bloqueados a nivel de BD por <c>trg_audit_logs_no_update</c>/
/// <c>trg_audit_logs_no_delete</c> (<c>prevent_audit_log_mutation()</c>) y
/// además por ACL (<c>procofa_app</c> sin GRANT de UPDATE/DELETE sobre esta
/// tabla) — doble enforcement a nivel de BD, fuera de alcance de EF
/// (Instrucción 03).
///
/// <see cref="AuditLog.IpAddress"/> mapea la columna física <c>inet</c>
/// como <c>string</c> (Domain agnóstico de tipos de infraestructura) vía
/// <c>.HasColumnType("inet")</c>; la conversión string↔inet de Npgsql debe
/// verificarse en la primera corrida real de integration tests contra
/// Postgres 18 (no ejecutable en este sandbox — NuGet restore bloqueado).
/// FK a <c>tenants</c> es <c>ON DELETE RESTRICT</c> (no <c>CASCADE</c>) —
/// la bitácora debe sobrevivir incluso ante intentos de eliminar el tenant;
/// se replica tal cual existe en la BD real.
/// </summary>
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
