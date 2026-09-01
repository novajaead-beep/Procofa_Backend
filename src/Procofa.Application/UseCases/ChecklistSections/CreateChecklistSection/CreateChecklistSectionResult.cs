namespace Procofa.Application.UseCases.ChecklistSections.CreateChecklistSection;

public enum CreateChecklistSectionError
{
    ValidationFailed,
    VersionNotFound,
    VersionPublished,
}

public sealed class CreateChecklistSectionResult
{
    public bool IsSuccess { get; }
    public CreateChecklistSectionError? Error { get; }
    public Guid? SectionId { get; }

    private CreateChecklistSectionResult(bool isSuccess, CreateChecklistSectionError? error, Guid? sectionId)
    {
        IsSuccess = isSuccess;
        Error = error;
        SectionId = sectionId;
    }

    public static CreateChecklistSectionResult Success(Guid sectionId) => new(true, null, sectionId);

    public static CreateChecklistSectionResult Failure(CreateChecklistSectionError error) => new(false, error, null);
}
