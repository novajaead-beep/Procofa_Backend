namespace Procofa.Application.UseCases.Criteria.GetCriterion;

public sealed record GetCriterionQuery(Guid ChecklistId, Guid VersionId, Guid SectionId, Guid CriterionId);
