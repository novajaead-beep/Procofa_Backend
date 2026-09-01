namespace Procofa.Application.UseCases.Checklists.CreateChecklist;

public sealed record CreateChecklistCommand(
    Guid? ProgramId,
    Guid? ProfileId,
    Guid? AuditTypeId,
    string? Name,
    string? Description);
