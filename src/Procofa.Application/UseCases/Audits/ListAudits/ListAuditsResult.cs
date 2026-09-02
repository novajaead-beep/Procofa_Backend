namespace Procofa.Application.UseCases.Audits.ListAudits;

public enum ListAuditsError
{
    ValidationFailed,
}

public sealed record AuditListItem(
    Guid Id,
    string Folio,
    Guid ClientId,
    Guid AuditedCompanyId,
    Guid? CompanySiteId,
    Guid AuditTypeId,
    Guid ProfileId,
    Guid StatusId,
    string Objective,
    DateOnly ScheduledDate,
    DateTime? StartedAtUtc,
    string ExecutionMode,
    DateTime CreatedAtUtc);

public sealed class ListAuditsResult(IReadOnlyList<AuditListItem> items, int page, int pageSize, int total)
{
    public bool IsSuccess { get; private init; } = true;
    public ListAuditsError? Error { get; private init; }
    public string? ErrorDetail { get; private init; }
    public IReadOnlyList<AuditListItem> Items { get; } = items;
    public int Page { get; } = page;
    public int PageSize { get; } = pageSize;
    public int Total { get; } = total;

    public static ListAuditsResult Failure(ListAuditsError error, string? errorDetail = null) =>
        new([], 0, 0, 0) { IsSuccess = false, Error = error, ErrorDetail = errorDetail };
}
