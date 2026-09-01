using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Checklists;

namespace Procofa.Application.UseCases.ChecklistSections.CreateChecklistSection;

public sealed class CreateChecklistSectionCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IChecklistVersionRepository checklistVersionRepository,
    IChecklistSectionRepository checklistSectionRepository)
{
    public Task<CreateChecklistSectionResult> HandleAsync(
        CreateChecklistSectionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Task.FromResult(CreateChecklistSectionResult.Failure(CreateChecklistSectionError.ValidationFailed));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var version = await checklistVersionRepository.GetByIdAsync(
                    tenantId, command.ChecklistId, command.VersionId, ct);
                if (version is null)
                {
                    return CreateChecklistSectionResult.Failure(CreateChecklistSectionError.VersionNotFound);
                }

                if (!version.IsEditable)
                {
                    return CreateChecklistSectionResult.Failure(CreateChecklistSectionError.VersionPublished);
                }

                var section = new ChecklistSection(
                    Guid.NewGuid(), tenantId, command.VersionId, command.Code, command.Name!, command.Description,
                    command.SortOrder);

                await checklistSectionRepository.AddAsync(section, ct);

                return CreateChecklistSectionResult.Success(section.Id);
            },
            cancellationToken);
    }
}
