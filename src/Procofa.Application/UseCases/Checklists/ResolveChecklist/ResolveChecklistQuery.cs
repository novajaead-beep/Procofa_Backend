namespace Procofa.Application.UseCases.Checklists.ResolveChecklist;

/// <summary><c>GET /api/checklists/resolve</c>. Cada campo acepta código de catálogo o GUID —
/// resuelto por <see cref="ResolveChecklistQueryHandler"/>.</summary>
public sealed record ResolveChecklistQuery(string? Program, string? Profile, string? AuditType);
