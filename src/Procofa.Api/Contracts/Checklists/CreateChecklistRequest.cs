namespace Procofa.Api.Contracts.Checklists;

public sealed record CreateChecklistRequest(
    Guid? ProgramId, Guid? ProfileId, Guid? AuditTypeId, string? Name, string? Description);

public sealed record CreateChecklistResponse(Guid Id);
