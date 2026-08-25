using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Enums;
using Procofa.Infrastructure.Persistence.Conversions.Enums;

namespace Procofa.Infrastructure.Persistence.Configurations.Audits;

/// <summary>
/// Mapeo fiel de <c>public.audits</c>. Incluye las colecciones owned
/// <c>Audit.Programs</c> (tabla <c>audit_programs</c>) y
/// <c>Audit.Team</c> (tabla <c>audit_team</c>, con el índice único parcial
/// <c>uq_audit_team_one_lead</c> que garantiza un solo LEAD por auditoría).
/// </summary>
public sealed class AuditConfiguration : IEntityTypeConfiguration<Audit>
{
    public void Configure(EntityTypeBuilder<Audit> builder)
    {
        builder.ToTable("audits", table =>
        {
            table.HasCheckConstraint(
                "ck_audits_execution_mode",
                "(execution_mode)::text = ANY (ARRAY['ONSITE','REMOTE','HYBRID']::text[])");
        });

        builder.HasKey(x => x.Id).HasName("audits_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(x => x.Folio).HasColumnName("folio").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ClientId).HasColumnName("client_id").IsRequired();
        builder.Property(x => x.AuditedCompanyId).HasColumnName("audited_company_id").IsRequired();
        builder.Property(x => x.CompanySiteId).HasColumnName("company_site_id");
        builder.Property(x => x.AuditTypeId).HasColumnName("audit_type_id").IsRequired();
        builder.Property(x => x.ProfileId).HasColumnName("profile_id").IsRequired();
        builder.Property(x => x.StatusId).HasColumnName("status_id").IsRequired();

        builder.Property(x => x.Objective).HasColumnName("objective").HasColumnType("text").IsRequired();
        builder.Property(x => x.Scope).HasColumnName("scope").HasColumnType("text").IsRequired();
        builder.Property(x => x.Methodology).HasColumnName("methodology").HasColumnType("text");

        builder.Property(x => x.ScheduledDate).HasColumnName("scheduled_date").IsRequired();
        builder.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(x => x.FinishedAtUtc).HasColumnName("finished_at_utc");
        builder.Property(x => x.ClosedAtUtc).HasColumnName("closed_at_utc");

        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(x => x.ValidatedByUserId).HasColumnName("validated_by_user_id");
        builder.Property(x => x.ValidatedAtUtc).HasColumnName("validated_at_utc");

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_audits_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        // Sin CHECK/trigger que fuerce CompanySiteId cuando ExecutionMode = Onsite:
        // la regla vive en Domain/Application (baseline V2.1 sección D).
        builder.Property(x => x.ExecutionMode)
            .HasColumnName("execution_mode")
            .HasConversion(new ExecutionModeConverter())
            .HasMaxLength(20)
            .IsRequired();


        builder.HasIndex(x => new { x.TenantId, x.Folio })
            .IsUnique()
            .HasDatabaseName("uq_audits_tenant_folio");

        builder.HasIndex(x => new { x.TenantId, x.ClientId, x.ScheduledDate })
            .HasDatabaseName("ix_audits_client")
            .IsDescending(false, false, true);

        builder.HasIndex(x => new { x.TenantId, x.AuditedCompanyId, x.ScheduledDate })
            .HasDatabaseName("ix_audits_company")
            .IsDescending(false, false, true);

        builder.HasIndex(x => new { x.TenantId, x.StatusId, x.ScheduledDate })
            .HasDatabaseName("ix_audits_status")
            .IsDescending(false, false, true);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_audits_tenant").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Client>().WithMany().HasForeignKey(x => x.ClientId).HasConstraintName("fk_audits_client").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AuditedCompany>().WithMany().HasForeignKey(x => x.AuditedCompanyId).HasConstraintName("fk_audits_company").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CompanySite>().WithMany().HasForeignKey(x => x.CompanySiteId).HasConstraintName("fk_audits_site").OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<AuditType>().WithMany().HasForeignKey(x => x.AuditTypeId).HasConstraintName("fk_audits_type").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Profile>().WithMany().HasForeignKey(x => x.ProfileId).HasConstraintName("fk_audits_profile").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AuditStatus>().WithMany().HasForeignKey(x => x.StatusId).HasConstraintName("fk_audits_status").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).HasConstraintName("fk_audits_created_by").OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ValidatedByUserId).HasConstraintName("fk_audits_validated_by")
            .OnDelete(DeleteBehavior.SetNull);

        builder.OwnsMany(x => x.Programs, ap =>
        {
            ap.ToTable("audit_programs");
            ap.WithOwner().HasForeignKey(x => x.AuditId).HasConstraintName("fk_audit_programs_audit");
            ap.HasKey(x => new { x.AuditId, x.ProgramId }).HasName("pk_audit_programs");

            ap.Property(x => x.TenantId).HasColumnName("tenant_id");
            ap.Property(x => x.AuditId).HasColumnName("audit_id");
            ap.Property(x => x.ProgramId).HasColumnName("program_id");

            // Instrucción 03.1, defecto 2: FK a tenants faltante.
            ap.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId).HasConstraintName("fk_audit_programs_tenant")
                .OnDelete(DeleteBehavior.Cascade);

            ap.HasOne<ComplianceProgram>()
                .WithMany()
                .HasForeignKey(x => x.ProgramId).HasConstraintName("fk_audit_programs_program")
                .OnDelete(DeleteBehavior.Restrict);

            ap.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.OwnsMany(x => x.Team, at =>
        {
            at.ToTable("audit_team", table =>
            {
                table.HasCheckConstraint(
                    "audit_team_audit_role_check",
                    "(audit_role)::text = ANY (ARRAY['LEAD','SUPPORT']::text[])");
            });
            at.WithOwner().HasForeignKey(x => x.AuditId).HasConstraintName("fk_audit_team_audit");
            at.HasKey(x => new { x.AuditId, x.UserId }).HasName("pk_audit_team");

            at.Property(x => x.TenantId).HasColumnName("tenant_id");
            at.Property(x => x.AuditId).HasColumnName("audit_id");
            at.Property(x => x.UserId).HasColumnName("user_id");

            // Instrucción 03.1, defecto 2: FK a tenants faltante.
            at.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId).HasConstraintName("fk_audit_team_tenant")
                .OnDelete(DeleteBehavior.Cascade);

            at.Property(x => x.AuditRole)
                .HasColumnName("audit_role")
                .HasConversion(new AuditTeamRoleConverter())
                .HasMaxLength(20)
                .IsRequired();


            at.Property(x => x.AssignedByUserId).HasColumnName("assigned_by_user_id");
            at.Property(x => x.AssignedAtUtc)
                .HasColumnName("assigned_at_utc")
                .HasDefaultValueSql("now()")
                .ValueGeneratedOnAdd();

            // Un solo LEAD por auditoría — índice único parcial físico.
            at.HasIndex(x => x.AuditId)
                .IsUnique()
                .HasDatabaseName("uq_audit_team_one_lead")
                .HasFilter("audit_role = 'LEAD'::character varying");

            at.HasIndex(x => new { x.TenantId, x.UserId, x.AuditId })
                .HasDatabaseName("ix_audit_team_user");

            at.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.UserId).HasConstraintName("fk_audit_team_user")
                .OnDelete(DeleteBehavior.Restrict);

            at.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.AssignedByUserId).HasConstraintName("fk_audit_team_assigned_by")
                .OnDelete(DeleteBehavior.SetNull);

            at.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(x => x.Programs).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Team).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
