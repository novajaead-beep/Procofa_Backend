using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Infrastructure;
using Procofa.Domain.Enums;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Infrastructure;

/// <summary>
/// Mapeo fiel de <c>public.idempotency_operations</c>. La BD real define
/// dos índices distintos sobre el mismo par de columnas
/// <c>(tenant_id, operation_id)</c> — la constraint única
/// <c>uq_idempotency_operation</c> y el índice de lectura
/// <c>ix_idempotency_operations_lookup</c> — se replican ambos con nombre
/// EF explícito (segundo argumento de <c>HasIndex</c>) para que EF los
/// materialice como dos índices físicos independientes en vez de fusionar
/// la configuración en uno solo.
/// </summary>
public sealed class IdempotencyOperationConfiguration : IEntityTypeConfiguration<IdempotencyOperation>
{
    public void Configure(EntityTypeBuilder<IdempotencyOperation> builder)
    {
        builder.ToTable("idempotency_operations", table =>
        {
            table.HasCheckConstraint(
                "idempotency_operations_status_check",
                "(status)::text = ANY (ARRAY['IN_PROGRESS','COMPLETED','FAILED']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("idempotency_operations_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(x => x.OperationId).HasColumnName("operation_id").IsRequired();
        builder.Property(x => x.OperationType).HasColumnName("operation_type").HasMaxLength(80).IsRequired();
        builder.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(64);
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(80);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(new IdempotencyOperationStatusConverter())
            .HasMaxLength(20)
            .HasDefaultValue(IdempotencyOperationStatus.InProgress)
            .IsRequired();


        builder.Property(x => x.ResponseStatusCode).HasColumnName("response_status_code");

        // jsonb — Domain se mantiene agnóstico de la librería de serialización.
        builder.Property(x => x.ResponsePayload).HasColumnName("response_payload").HasColumnType("jsonb");

        builder.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.TenantId, x.OperationId }, "uq_idempotency_operation")
            .IsUnique()
            .HasDatabaseName("uq_idempotency_operation");

        // Índice adicional replicado tal cual existe en la BD real, aunque
        // sea redundante con el índice único anterior (no se "optimiza").
        builder.HasIndex(x => new { x.TenantId, x.OperationId }, "ix_idempotency_operations_lookup")
            .HasDatabaseName("ix_idempotency_operations_lookup");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_idempotency_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).HasConstraintName("fk_idempotency_user").OnDelete(DeleteBehavior.Cascade);
    }
}
