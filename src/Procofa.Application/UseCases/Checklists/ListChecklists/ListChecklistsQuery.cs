namespace Procofa.Application.UseCases.Checklists.ListChecklists;

public sealed record ListChecklistsQuery(
    string? Search,
    Guid? ProgramId,
    Guid? ProfileId,
    Guid? AuditTypeId,
    bool? IsActive,
    int Page,
    int PageSize);
