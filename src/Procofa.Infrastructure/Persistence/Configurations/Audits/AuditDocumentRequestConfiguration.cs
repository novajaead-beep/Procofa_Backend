using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Identity;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Audits;

/// <summary>Mapeo fiel de <c>public.audit_document_requests</c>.</summary>
public sealed class AuditDocumentRequestConfiguration : IEntityTypeConfiguration<AuditDocumentRequest>
{
    public void Configure(EntityTypeBuilder<AuditDocumentRequest> builder)
    {
        builder.ToTable("audit_document_requests", table =>
        {
            table.HasCheckConstraint(
                "audit_document_requests_status_check",
                "(status)::text = ANY (ARRAY['PENDIENTE','ENTREGADO','VALIDADO','RECHAZADO','CANCELADO']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("audit_document_requests_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.AuditId).HasColumnName("audit_id").IsRequired();
        builder.Property(x => x.RequestedByUserId).HasColumnName("requested_by_user_id").IsRequired();

        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(x => x.DueDate).HasColumnName("due_date");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(new DocumentRequestStatusConverter())
            .HasMaxLength(30)
            .HasDefaultValue(Domain.Enums.DocumentRequestStatus.Pendiente)
            .IsRequired();


        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_document_requests_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_document_requests_tenant").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Audit>().WithMany().HasForeignKey(x => x.AuditId).HasConstraintName("fk_document_requests_audit").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedByUserId).HasConstraintName("fk_document_requests_requested_by").OnDelete(DeleteBehavior.Restrict);
    }
}
