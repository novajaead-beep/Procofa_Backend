namespace Procofa.Application.UseCases.Criteria.DeleteCriterion;

public sealed record DeleteCriterionCommand(Guid ChecklistId, Guid VersionId, Guid SectionId, Guid CriterionId);
