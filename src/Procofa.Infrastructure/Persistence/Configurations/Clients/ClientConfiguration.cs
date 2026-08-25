using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Configurations.Clients;

/// <summary>
/// Mapeo fiel de <c>public.clients</c>. Incluye la colección owned
/// <c>Client.Programs</c> (tabla <c>public.client_programs</c>, PK
/// compuesta <c>(client_id, program_id)</c>, sin columna <c>id</c>).
/// </summary>
public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");

        builder.HasKey(x => x.Id).HasName("clients_pkey");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();

        builder.Property(x => x.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.TradeName).HasColumnName("trade_name").HasMaxLength(200);
        builder.Property(x => x.TaxId).HasColumnName("tax_id").HasMaxLength(30);
        builder.Property(x => x.Industry).HasColumnName("industry").HasMaxLength(150);
        builder.Property(x => x.CompanyType).HasColumnName("company_type").HasMaxLength(100);
        builder.Property(x => x.Notes).HasColumnName("notes").HasColumnType("text");

        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();

        // Trigger trg_clients_updated_at. EF nunca la escribe.
        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate();

        builder.HasIndex(x => new { x.TenantId, x.TaxId })
            .IsUnique()
            .HasDatabaseName("uq_clients_tenant_tax_id")
            .HasFilter("tax_id IS NOT NULL");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("ix_clients_tenant");

        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).HasConstraintName("fk_clients_tenant").OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(x => x.Programs, cp =>
        {
            cp.ToTable("client_programs");
            cp.WithOwner().HasForeignKey(x => x.ClientId).HasConstraintName("fk_client_programs_client");
            cp.HasKey(x => new { x.ClientId, x.ProgramId }).HasName("pk_client_programs");

            cp.Property(x => x.TenantId).HasColumnName("tenant_id");
            cp.Property(x => x.ClientId).HasColumnName("client_id");
            cp.Property(x => x.ProgramId).HasColumnName("program_id");

            // Instrucción 03.1, defecto 2: FK a tenants faltante.
            cp.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId).HasConstraintName("fk_client_programs_tenant")
                .OnDelete(DeleteBehavior.Cascade);

            cp.HasOne<ComplianceProgram>()
                .WithMany()
                .HasForeignKey(x => x.ProgramId).HasConstraintName("fk_client_programs_program")
                .OnDelete(DeleteBehavior.Restrict);

            cp.UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(x => x.Programs).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
