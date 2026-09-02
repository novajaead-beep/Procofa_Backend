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
/// Invariantes enforzadas por este agregado:
/// <list type="bullet">
/// <item><see cref="ExecutionMode"/> ↔ <c>company_site_id</c>: sin CHECK/trigger en BD, validado
/// en el constructor y en <see cref="UpdateDetails"/> (ver <c>EnsureExecutionModeMatchesSite</c>).</item>
/// <item>A lo más un <see cref="AuditTeamRole.Lead"/> en <see cref="Team"/>: garantizado por el
/// índice único parcial <c>uq_audit_team_one_lead</c> (a nivel BD, no revalidado aquí). Exigir
/// *al menos* un LEAD queda deliberadamente fuera de <see cref="ReplaceTeam"/> — no hay todavía un
/// caso de uso de "planificación completa" al que atarlo, y exigirlo en cada reemplazo bloquearía
/// construir el equipo por etapas.</item>
/// <item>No cerrar con criterios obligatorios sin evaluar: espeja el trigger
/// <c>trg_audits_validate_close</c> (<c>validate_audit_before_close()</c>) — todavía sin caso de
/// uso de cierre implementado.</item>
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

    /// <summary>Señal física de arranque de la ejecución — el grafo completo de transición de
    /// estados (<see cref="AuditStatus"/>) no está definido todavía (baseline V2.1, hallazgo 🟡
    /// sección C), pero <see cref="StartedAtUtc"/> ya existe físicamente y es inequívoca: una
    /// auditoría que arrancó ejecución deja de admitir cambios de planificación.</summary>
    public bool IsEditable => StartedAtUtc is null;

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
        EnsureExecutionModeMatchesSite(executionMode, companySiteId);

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

    /// <summary>Defensa en profundidad — Application ya debe validar <see cref="IsEditable"/>
    /// antes de llegar aquí; este método existe para que el propio agregado nunca acepte una
    /// mutación inconsistente con su estado.</summary>
    public void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new InvalidOperationException(
                "La auditoría ya inició ejecución: no admite cambios de planificación.");
        }
    }

    /// <summary>Actualiza los datos de planificación editables. <see cref="ClientId"/> es
    /// inmutable post-creación — no forma parte de esta operación.</summary>
    public void UpdateDetails(
        Guid auditedCompanyId,
        Guid? companySiteId,
        Guid auditTypeId,
        Guid profileId,
        string objective,
        string scope,
        string? methodology,
        DateOnly scheduledDate,
        ExecutionMode executionMode)
    {
        EnsureEditable();
        EnsureExecutionModeMatchesSite(executionMode, companySiteId);

        AuditedCompanyId = auditedCompanyId;
        CompanySiteId = companySiteId;
        AuditTypeId = auditTypeId;
        ProfileId = profileId;
        Objective = objective;
        Scope = scope;
        Methodology = methodology;
        ScheduledDate = scheduledDate;
        ExecutionMode = executionMode;
    }

    /// <summary>Reemplazo transaccional completo de <see cref="Programs"/> — los ids repetidos se
    /// deduplican silenciosamente (ningún dato adicional distingue dos referencias al mismo
    /// programa; tratarlo como error obligaría a Application a repetir la misma deduplicación
    /// antes de llamar aquí).</summary>
    public void ReplacePrograms(IReadOnlyCollection<Guid> programIds)
    {
        EnsureEditable();

        _programs.Clear();
        foreach (var programId in programIds.Distinct())
        {
            _programs.Add(new AuditProgram(TenantId, Id, programId));
        }
    }

    /// <summary>Reemplazo transaccional completo de <see cref="Team"/> — admite construir el
    /// equipo por etapas (incluida una colección vacía, o solo <see cref="AuditTeamRole.Support"/>
    /// sin <see cref="AuditTeamRole.Lead"/> todavía): esta instrucción no define un caso de uso de
    /// "planificación completa" al que atar la exigencia de al menos un LEAD, así que exigirlo en
    /// cada reemplazo bloquearía una edición parcial legítima. Un <c>userId</c> duplicado, o más de
    /// un <see cref="AuditTeamRole.Lead"/>, son invariantes que Application debe rechazar ANTES de
    /// llegar aquí (mismo criterio que <see cref="EnsureEditable"/>) — este método los revalida como
    /// defensa en profundidad, nunca como el único punto de control.</summary>
    public void ReplaceTeam(IReadOnlyCollection<(Guid UserId, AuditTeamRole Role)> members, Guid? assignedByUserId)
    {
        EnsureEditable();

        if (members.Select(m => m.UserId).Distinct().Count() != members.Count)
        {
            throw new InvalidOperationException("El equipo auditor no puede repetir el mismo usuario.");
        }

        if (members.Count(m => m.Role == AuditTeamRole.Lead) > 1)
        {
            throw new InvalidOperationException("El equipo auditor admite como máximo un LEAD.");
        }

        _team.Clear();
        foreach (var member in members)
        {
            _team.Add(new AuditTeamMember(TenantId, Id, member.UserId, member.Role, assignedByUserId));
        }
    }

    /// <summary>Sin CHECK/trigger equivalente en BD (verificado contra el dump: <c>company_site_id</c>
    /// es nullable a nivel de columna, sin CHECK cruzado con <c>execution_mode</c>) — ver <see
    /// cref="ExecutionMode"/>. ONSITE/HYBRID exigen sede; REMOTE la deja opcional.</summary>
    private static void EnsureExecutionModeMatchesSite(ExecutionMode executionMode, Guid? companySiteId)
    {
        if (executionMode is ExecutionMode.Onsite or ExecutionMode.Hybrid && companySiteId is null)
        {
            throw new InvalidOperationException(
                $"execution_mode = {executionMode} requiere company_site_id.");
        }
    }
}
