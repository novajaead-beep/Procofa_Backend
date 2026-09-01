namespace Procofa.Api.Contracts.Checklists;

public sealed record ChecklistDetailResponse(
    Guid Id, Guid ProgramId, Guid ProfileId, Guid? AuditTypeId, string Name, string? Description, bool IsActive,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
