namespace Procofa.Domain.Entities.Findings;

/// <summary>
/// Entrada de bitácora de seguimiento de un <see cref="Finding"/>, que
/// opcionalmente puede taggear una <see cref="CorrectiveAction"/> concreta
/// sin pertenecerle. Entidad independiente con <c>DbSet</c> propio.
/// Tabla física: <c>finding_followups</c>, tenant-scoped, RLS+FORCE RLS,
/// <c>ON DELETE CASCADE</c> desde <c>findings</c>,
/// <c>ON DELETE SET NULL</c> desde <c>corrective_actions</c>.
///
/// <see cref="EventType"/> es <c>varchar(50)</c> SIN CHECK constraint en la
/// BD real — texto libre, no un enum (a diferencia de otras columnas
/// "*_type" de este dominio que sí tienen CHECK).
/// </summary>
public sealed class FindingFollowup
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FindingId { get; private set; }
    public Guid? CorrectiveActionId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string? Comment { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private FindingFollowup() { }

    public FindingFollowup(
        Guid id,
        Guid tenantId,
        Guid findingId,
        Guid? correctiveActionId,
        Guid authorUserId,
        string eventType,
        string? comment)
    {
        Id = id;
        TenantId = tenantId;
        FindingId = findingId;
        CorrectiveActionId = correctiveActionId;
        AuthorUserId = authorUserId;
        EventType = eventType;
        Comment = comment;
    }
}
