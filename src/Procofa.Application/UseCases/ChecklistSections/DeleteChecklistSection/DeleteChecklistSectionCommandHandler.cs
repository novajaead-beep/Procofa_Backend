using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.ChecklistSections.DeleteChecklistSection;

/// <summary>Delete físico — permitido porque <c>criteria.checklist_section_id</c> tiene <c>ON
/// DELETE RESTRICT</c> en el baseline: la BD ya impide borrar una sección con criterios; este
/// handler solo adelanta esa misma regla para devolver 409 limpio en vez de dejar que la
/// excepción Postgres llegue a Api.</summary>
public sealed class DeleteChecklistSectionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository,
    ICriterionRepository criterionRepository)
{
    public Task<DeleteChecklistSectionResult> HandleAsync(
        DeleteChecklistSectionCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, command.ChecklistId, command.VersionId, ct);
                if (version is null)
                {
                    return DeleteChecklistSectionResult.Failure(DeleteChecklistSectionError.NotFound);
                }

                var section = await checklistSectionRepository.GetByIdAsync(
                    tenantId, command.VersionId, command.SectionId, ct);
                if (section is null)
                {
                    return DeleteChecklistSectionResult.Failure(DeleteChecklistSectionError.NotFound);
                }

                if (!version.IsEditable)
                {
                    return DeleteChecklistSectionResult.Failure(DeleteChecklistSectionError.VersionPublished);
                }

                if (await criterionRepository.ExistsForSectionAsync(tenantId, section.Id, ct))
                {
                    return DeleteChecklistSectionResult.Failure(DeleteChecklistSectionError.HasCriteria);
                }

                await checklistSectionRepository.RemoveAsync(section, ct);

                return DeleteChecklistSectionResult.Success();
            },
            cancellationToken);
    }
}
