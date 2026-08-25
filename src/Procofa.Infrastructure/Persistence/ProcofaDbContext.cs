using Microsoft.EntityFrameworkCore;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Catalogs;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Findings;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Entities.Infrastructure;
using Procofa.Domain.Entities.Reports;
using Procofa.Infrastructure.Persistence.Conversions;

namespace Procofa.Infrastructure.Persistence;

/// <summary>
/// DbContext único de Procofa — representa fielmente el baseline PostgreSQL
/// V2.1 (48 tablas físicas: 42 con <see cref="DbSet{TEntity}"/> propio + 6
/// tipos poseídos (<c>OwnsMany</c>) anidados en Role/User/Client/Audit, sin
/// <c>DbSet</c> independiente — ver Instrucción 03).
///
/// Responsabilidades explícitamente FUERA de este tipo:
/// <list type="bullet">
/// <item>No conoce <c>HttpContext</c> ni ningún concepto de capa HTTP.</item>
/// <item>No decide el tenant efectivo ni ejecuta <c>SET LOCAL</c>/
/// <c>set_config</c> — eso es responsabilidad exclusiva de
/// <c>ITenantUnitOfWork</c> (implementado en <c>Persistence/Tenancy</c>),
/// que reutiliza esta MISMA instancia scoped de <see cref="ProcofaDbContext"/>
/// para abrir la transacción y aplicar el tenant antes de que cualquier
/// query la use.</item>
/// <item>No llama <c>Database.EnsureCreated()</c>/<c>EnsureDeleted()</c> ni
/// <c>Database.Migrate()</c> en ningún lado — el ciclo de vida del esquema
/// físico se gestiona exclusivamente vía <c>dotnet ef migrations</c>
/// ejecutado manualmente contra una BD desechable (Testcontainers en tests,
/// pipeline de despliegue en producción), nunca por este proceso en
/// arranque (regla de seguridad de Instrucción 03).</item>
/// </list>
///
/// Nombres físicos: NO se usa una convención global de snake_case (paquete
/// de terceros) — cada <c>IEntityTypeConfiguration&lt;T&gt;</c> en
/// <c>Persistence/Configurations</c> fija explícitamente
/// <c>.ToTable(...)</c>/<c>.HasColumnName(...)</c> con el nombre físico
/// exacto tal como existe en <c>procofa_bdFinal.sql</c> — descubiertas
/// automáticamente vía <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>.
///
/// Estrategia UTC: ver <see cref="UtcDateTimeConverter"/>, aplicada
/// globalmente en <see cref="ConfigureConventions"/>.
/// </summary>
public sealed class ProcofaDbContext : DbContext
{
    public ProcofaDbContext(DbContextOptions<ProcofaDbContext> options)
        : base(options)
    {
    }

    // ---- Identity (7 DbSet + 3 tipos poseídos sin DbSet: role_permissions,
    // user_roles, user_client_access) ----
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    // ---- Clients (4 DbSet + 1 tipo poseído sin DbSet: client_programs) ----
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<AuditedCompany> AuditedCompanies => Set<AuditedCompany>();
    public DbSet<CompanySite> CompanySites => Set<CompanySite>();
    public DbSet<ClientContact> ClientContacts => Set<ClientContact>();

    // ---- Catalogs (9 DbSet) ----
    public DbSet<ComplianceProgram> CompliancePrograms => Set<ComplianceProgram>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<AuditType> AuditTypes => Set<AuditType>();
    public DbSet<AuditStatus> AuditStatuses => Set<AuditStatus>();
    public DbSet<ComplianceStatus> ComplianceStatuses => Set<ComplianceStatus>();
    public DbSet<FindingType> FindingTypes => Set<FindingType>();
    public DbSet<FindingPriority> FindingPriorities => Set<FindingPriority>();
    public DbSet<FindingStatus> FindingStatuses => Set<FindingStatus>();
    public DbSet<CorrectiveActionStatus> CorrectiveActionStatuses => Set<CorrectiveActionStatus>();

    // ---- Checklists (4 DbSet) ----
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistVersion> ChecklistVersions => Set<ChecklistVersion>();
    public DbSet<ChecklistSection> ChecklistSections => Set<ChecklistSection>();
    public DbSet<Criterion> Criteria => Set<Criterion>();

    // ---- Audits (5 DbSet + 2 tipos poseídos sin DbSet: audit_programs, audit_team) ----
    public DbSet<Audit> Audits => Set<Audit>();
    public DbSet<AuditChecklist> AuditChecklists => Set<AuditChecklist>();
    public DbSet<AuditCriterion> AuditCriteria => Set<AuditCriterion>();
    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<AuditDocumentRequest> AuditDocumentRequests => Set<AuditDocumentRequest>();

    // ---- Findings (4 DbSet) ----
    public DbSet<AuditEvidence> AuditEvidences => Set<AuditEvidence>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<CorrectiveAction> CorrectiveActions => Set<CorrectiveAction>();
    public DbSet<FindingFollowup> FindingFollowups => Set<FindingFollowup>();

    // ---- Reports (5 DbSet) ----
    public DbSet<AuditResult> AuditResults => Set<AuditResult>();
    public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();
    public DbSet<ReportTemplateVersion> ReportTemplateVersions => Set<ReportTemplateVersion>();
    public DbSet<AuditReport> AuditReports => Set<AuditReport>();
    public DbSet<AuditSignatory> AuditSignatories => Set<AuditSignatory>();

    // ---- Infraestructura (4 DbSet) ----
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<IdempotencyOperation> IdempotencyOperations => Set<IdempotencyOperation>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Estrategia UTC única — ver UtcDateTimeConverter. Se aplica a TODA
        // propiedad DateTime/DateTime? del modelo sin necesidad de repetir
        // .HasConversion(...) en cada una de las 42+ configuraciones.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcofaDbContext).Assembly);
    }
}
