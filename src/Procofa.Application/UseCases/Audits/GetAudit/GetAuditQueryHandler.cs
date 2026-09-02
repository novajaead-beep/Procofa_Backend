using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Audits;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Clients;

namespace Procofa.Application.UseCases.Audits.GetAudit;

public sealed class GetAuditQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditRepository auditRepository,
    IProgramRepository programRepository,
    IUserRepository userRepository,
    IAuditChecklistRepository auditChecklistRepository,
    ICurrentUser currentUser)
{
    public Task<GetAuditResult> HandleAsync(GetAuditQuery query, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var audit = await auditRepository.GetByIdAsync(tenantId, query.AuditId, ct);
                if (audit is null)
                {
                    return GetAuditResult.NotFound();
                }

                var scope = await ClientAccessScope.ResolveAsync(currentUser, tenantId, userRepository, ct);
                if (!ClientAccessScope.IsVisible(scope, audit.ClientId))
                {
                    return GetAuditResult.NotFound();
                }

                var programCodes = await programRepository.GetCodesByIdsAsync(
                    audit.Programs.Select(p => p.ProgramId).ToArray(), ct);

                var team = audit.Team
                    .Select(m => new GetAuditTeamMemberItem(
                        m.UserId, AuditTeamRoleParser.ToRequestString(m.AuditRole), m.AssignedByUserId,
                        m.AssignedAtUtc))
                    .ToArray();

                var checklistDetails = await auditChecklistRepository.ListDetailedByAuditAsync(tenantId, audit.Id, ct);
                var checklists = checklistDetails
                    .Select(d => new GetAuditChecklistItem(
                        d.AuditChecklistId, d.ChecklistId, d.ChecklistVersionId, d.VersionNumber, d.ChecklistName))
                    .ToArray();

                return GetAuditResult.Success(
                    audit.Id, audit.Folio, audit.ClientId, audit.AuditedCompanyId, audit.CompanySiteId,
                    audit.AuditTypeId, audit.ProfileId, audit.StatusId, audit.Objective, audit.Scope,
                    audit.Methodology, audit.ScheduledDate, audit.StartedAtUtc, audit.FinishedAtUtc,
                    audit.ClosedAtUtc, ExecutionModeParser.ToRequestString(audit.ExecutionMode), audit.IsEditable,
                    programCodes, team, checklists, audit.CreatedAtUtc, audit.UpdatedAtUtc);
            },
            cancellationToken);
    }
}
