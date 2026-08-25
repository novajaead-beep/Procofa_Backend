namespace Procofa.Domain.Entities.Findings;

/// <summary>
/// Acción correctiva y su ciclo de vida (responder, validar, cerrar) para
/// un <see cref="Finding"/>. Aggregate Root propio, separado de
/// <see cref="Finding"/> (evidencia: <see cref="LockVersion"/> propio — un
/// CLIENTE respondiendo esta acción no debe contender con un AUDITOR_LIDER
/// validando otro aspecto del mismo Finding; baseline V2.1 sección F).
///
/// Tabla física: <c>corrective_actions</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>findings</c>.
/// Invariante <c>ck_corrective_action_responsible</c>: al menos uno de
/// <see cref="ResponsibleUserId"/>/<see cref="ResponsibleContactId"/> debe
/// estar presente — replicada como <c>.HasCheckConstraint(...)</c> en
/// <c>CorrectiveActionConfiguration</c>, no enforzada en el constructor de
/// esta instrucción (persistencia, no casos de uso).
/// Índice parcial <c>ix_corrective_actions_commitment_date WHERE completed_at_utc IS NULL</c>.
/// </summary>
public sealed class CorrectiveAction
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FindingId { get; private set; }
    public Guid StatusId { get; private set; }
    public string Description { get; private set; } = null!;
    public Guid? ResponsibleUserId { get; private set; }
    public Guid? ResponsibleContactId { get; private set; }
    public DateOnly CommitmentDate { get; private set; }
    public string? CompletionNotes { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? ValidatedByUserId { get; private set; }
    public DateTime? ValidatedAtUtc { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// <c>bigint DEFAULT 1 NOT NULL CHECK (lock_version &gt; 0)</c>.
    /// Ver <c>ConcurrencyTokenInterceptor</c>.
    /// </summary>
    public long LockVersion { get; private set; } = 1;

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Trigger <c>trg_corrective_actions_updated_at</c>. EF nunca la escribe.</summary>
    public DateTime UpdatedAtUtc { get; private set; }

    private CorrectiveAction() { }

    public CorrectiveAction(
        Guid id,
        Guid tenantId,
        Guid findingId,
        Guid statusId,
        string description,
        Guid? responsibleUserId,
        Guid? responsibleContactId,
        DateOnly commitmentDate,
        Guid createdByUserId)
    {
        Id = id;
        TenantId = tenantId;
        FindingId = findingId;
        StatusId = statusId;
        Description = description;
        ResponsibleUserId = responsibleUserId;
        ResponsibleContactId = responsibleContactId;
        CommitmentDate = commitmentDate;
        CreatedByUserId = createdByUserId;
        LockVersion = 1;
    }
}
