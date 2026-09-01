namespace Procofa.Application.UseCases.Checklists.GetChecklist;

public enum GetChecklistError
{
    NotFound,
}

public sealed class GetChecklistResult
{
    public bool IsSuccess { get; }
    public GetChecklistError? Error { get; }
    public Guid Id { get; }
    public Guid ProgramId { get; }
    public Guid ProfileId { get; }
    public Guid? AuditTypeId { get; }
    public string Name { get; } = string.Empty;
    public string? Description { get; }
    public bool IsActive { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; }

    private GetChecklistResult(bool isSuccess, GetChecklistError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    private GetChecklistResult(
        Guid id, Guid programId, Guid profileId, Guid? auditTypeId, string name, string? description,
        bool isActive, DateTime createdAtUtc, DateTime updatedAtUtc)
        : this(true, null)
    {
        Id = id;
        ProgramId = programId;
        ProfileId = profileId;
        AuditTypeId = auditTypeId;
        Name = name;
        Description = description;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GetChecklistResult Success(
        Guid id, Guid programId, Guid profileId, Guid? auditTypeId, string name, string? description,
        bool isActive, DateTime createdAtUtc, DateTime updatedAtUtc) =>
        new(id, programId, profileId, auditTypeId, name, description, isActive, createdAtUtc, updatedAtUtc);

    public static GetChecklistResult NotFound() => new(false, GetChecklistError.NotFound);
}
