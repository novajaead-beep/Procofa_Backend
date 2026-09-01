namespace Procofa.Api.Contracts.Checklists;

public sealed record UpdateChecklistRequest(
    Guid? ProgramId, Guid? ProfileId, Guid? AuditTypeId, string? Name, string? Description);
