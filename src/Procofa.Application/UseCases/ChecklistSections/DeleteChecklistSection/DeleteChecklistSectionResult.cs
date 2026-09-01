namespace Procofa.Application.UseCases.ChecklistSections.DeleteChecklistSection;

public enum DeleteChecklistSectionError
{
    NotFound,
    VersionPublished,
    HasCriteria,
}

public sealed class DeleteChecklistSectionResult
{
    public bool IsSuccess { get; }
    public DeleteChecklistSectionError? Error { get; }

    private DeleteChecklistSectionResult(bool isSuccess, DeleteChecklistSectionError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static DeleteChecklistSectionResult Success() => new(true, null);

    public static DeleteChecklistSectionResult Failure(DeleteChecklistSectionError error) => new(false, error);
}
