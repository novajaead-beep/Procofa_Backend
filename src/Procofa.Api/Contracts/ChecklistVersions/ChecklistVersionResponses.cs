namespace Procofa.Api.Contracts.ChecklistVersions;

public sealed record ChecklistVersionListItemResponse(
    Guid Id, int VersionNumber, string Status, DateTime? PublishedAtUtc, DateTime CreatedAtUtc);

public sealed record ChecklistVersionListResponse(IReadOnlyCollection<ChecklistVersionListItemResponse> Items);

public sealed record ChecklistVersionDetailResponse(
    Guid Id, int VersionNumber, string Status, string? ChangeNotes, DateTime? PublishedAtUtc,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
