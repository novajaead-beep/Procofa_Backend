namespace Procofa.Application.UseCases.Audits.ReplaceAuditChecklists;

/// <summary><c>PUT /api/audits/{auditId}/checklists</c>. <see cref="ChecklistIds"/> llega
/// explícito por id — sin auto-resolución mágica sobre todos los programas de la auditoría; cada
/// id se valida individualmente contra Program/Profile/AuditType y se resuelve a su última
/// <c>checklist_version</c> PUBLISHED.</summary>
public sealed record ReplaceAuditChecklistsCommand(Guid AuditId, IReadOnlyCollection<Guid>? ChecklistIds);
