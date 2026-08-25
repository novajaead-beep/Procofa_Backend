using Procofa.Domain.Entities.Audits.ValueObjects;
using Procofa.Domain.Enums;

namespace Procofa.Domain.Entities.Audits;

/// <summary>
/// Instancia de auditoría — planeación, ejecución, cierre. Aggregate Root.
/// Tabla física: <c>audits</c>, tenant-scoped, RLS+FORCE RLS.
///
/// Posee <see cref="Programs"/> (tabla <c>audit_programs</c>) y
/// <see cref="Team"/> (tabla <c>audit_team</c>) — ambas PK compuesta sin
/// columna <c>id</c> → colecciones owned, sin <c>DbSet</c> propio.
///
/// <c>AuditChecklist</c>, <c>AuditDocumentRequest</c>, <c>AuditResult</c> y
/// <c>AuditSignatory</c> están conceptualmente dentro del límite
/// transaccional de este aggregate (baseline V2.1 sección F) pero se
/// modelan como entidades independientes con <c>DbSet</c> propio — algunas
/// (<c>AuditChecklist</c>, <c>AuditDocumentRequest</c>) porque son
/// referenciadas por FK desde otros aggregates
/// (<c>audit_criteria.audit_checklist_id</c>,
/// <c>audit_evidences.document_request_id</c>); las demás por uniformidad
/// del criterio de mapeo (toda tabla con columna <c>id</c> propia → entidad
/// independiente con <c>DbSet</c>, ver nota en <c>ProcofaDbContext</c>).
///
/// Invariantes reflejadas (no enforzadas aún en Domain — esta instrucción es
/// de persistencia, no de casos de uso):
/// <list type="bullet">
/// <item><see cref="ExecutionMode"/> ↔ <c>company_site_id</c>: sin CHECK/trigger
/// en BD: la regla vive en Domain/Application (futuro).</item>
/// <item>Un solo <see cref="AuditTeamRole.Lead"/> en <see cref="Team"/>: espeja
/// el índice único parcial <c>uq_audit_team_one_lead</c>.</item>
/// <item>No cerrar con criterios obligatorios sin evaluar: espeja el trigger
/// <c>trg_audits_validate_close</c> (<c>validate_audit_before_close()</c>).</item>
/// </list>
///
/// Invariante <c>(tenant_id, folio)</c> único — ver <c>uq_audits_tenant_folio</c>
/// en <c>AuditConfiguration</c>.
/// </summary>
public sealed class Audit
{
    private readonly List<AuditProgram> _programs = [];
    private readonly List<AuditTeamMember> _team = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Folio { get; private set; } = null!;
    public Guid ClientId { get; private set; }
    public Guid AuditedCompanyId { get; private set; }
    public Guid? CompanySiteId { get; private set; }
    public Guid AuditTypeId { get; private set; }
    public Guid ProfileId { get; private set; }
    public Guid StatusId { get; private set; }
    public string Objective { get; private set; } = null!;
    public string Scope { get; private set; } = null!;
    public string? Methodology { get; private set; }
    public DateOnly ScheduledDate { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? FinishedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? ValidatedByUserId { get; private set; }
    public DateTime? ValidatedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_audits_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    public ExecutionMode ExecutionMode { get; private set; }

    public IReadOnlyCollection<AuditProgram> Programs => _programs.AsReadOnly();
    public IReadOnlyCollection<AuditTeamMember> Team => _team.AsReadOnly();

    private Audit() { }

    public Audit(
        Guid id,
        Guid tenantId,
        string folio,
        Guid clientId,
        Guid auditedCompanyId,
        Guid? companySiteId,
        Guid auditTypeId,
        Guid profileId,
        Guid statusId,
        string objective,
        string scope,
        string? methodology,
        DateOnly scheduledDate,
        Guid createdByUserId,
        ExecutionMode executionMode)
    {
        Id = id;
        TenantId = tenantId;
        Folio = folio;
        ClientId = clientId;
        AuditedCompanyId = auditedCompanyId;
        CompanySiteId = companySiteId;
        AuditTypeId = auditTypeId;
        ProfileId = profileId;
        StatusId = statusId;
        Objective = objective;
        Scope = scope;
        Methodology = methodology;
        ScheduledDate = scheduledDate;
        CreatedByUserId = createdByUserId;
        ExecutionMode = executionMode;
    }
}
