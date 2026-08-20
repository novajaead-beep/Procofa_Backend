using Microsoft.EntityFrameworkCore;
using Procofa.Domain.Audits;

namespace Procofa.Adapters.Persistence.PostgreSQL;

/// <summary>DbContext del núcleo de Auditorías. Los mapeos detallados viven en /Configurations (IEntityTypeConfiguration&lt;T&gt;).</summary>
public sealed class ProcofaDbContext : DbContext
{
    public ProcofaDbContext(DbContextOptions<ProcofaDbContext> options) : base(options) { }

    public DbSet<AuditPlan> AuditPlans => Set<AuditPlan>();
    public DbSet<CriterionSnapshot> CriterionSnapshots => Set<CriterionSnapshot>();
    public DbSet<AuditResult> AuditResults => Set<AuditResult>();
    public DbSet<Finding> Findings => Set<Finding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcofaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    // TODO (Semana 1-2, ver calendario SRS):
    //  - IEntityTypeConfiguration<AuditPlan>: mapear _teamMemberIds y _checklist como colecciones de
    //    backing field (UsePropertyAccessMode(PropertyAccessMode.Field)); RowVersion -> UseXminAsConcurrencyToken().
    //  - IEntityTypeConfiguration<AuditResult>: UNIQUE(criterion_snapshot_id); RowVersion -> UseXminAsConcurrencyToken().
    //  - IEntityTypeConfiguration<Finding>: UNIQUE(audit_result_id).
    //  - Alinear nombres de tabla/columna (snake_case) con db/scripts/001_core_schema.sql.
}
