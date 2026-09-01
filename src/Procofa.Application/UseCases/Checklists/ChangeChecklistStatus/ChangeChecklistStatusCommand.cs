namespace Procofa.Application.UseCases.Checklists.ChangeChecklistStatus;

public sealed record ChangeChecklistStatusCommand(Guid ChecklistId, bool IsActive);
