namespace Procofa.Application.UseCases.Checklists.UpdateChecklist;

public sealed record UpdateChecklistCommand(
    Guid ChecklistId,
    Guid? ProgramId,
    Guid? ProfileId,
    Guid? AuditTypeId,
    string? Name,
    string? Description);
