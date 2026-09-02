using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Audits;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Enums;

namespace Procofa.Application.UseCases.Audits.ReplaceAuditTeam;

/// <summary>Caso de uso <c>PUT /api/audits/{auditId}/team</c>. ADMIN no auto-asigna ni auto-remueve
/// nada — el equipo final es exactamente lo que llega en <see
/// cref="ReplaceAuditTeamCommand.Members"/>, incluida una colección vacía o solo SUPPORT: el
/// reemplazo admite construir el equipo por etapas. No se exige al menos un LEAD aquí — esta
/// instrucción no define un caso de uso de "planificación completa" al que atar esa exigencia (la
/// BD sí garantiza <b>a lo más</b> un LEAD vía <c>uq_audit_team_one_lead</c>).</summary>
public sealed class ReplaceAuditTeamCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditRepository auditRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    public Task<ReplaceAuditTeamResult> HandleAsync(
        ReplaceAuditTeamCommand command, CancellationToken cancellationToken)
    {
        var members = command.Members ?? [];

        if (members.Select(m => m.UserId).Distinct().Count() != members.Count)
        {
            return Task.FromResult(ReplaceAuditTeamResult.Failure(
                ReplaceAuditTeamError.DuplicateUser, "El equipo auditor no puede repetir el mismo usuario."));
        }

        var parsedMembers = new List<(Guid UserId, AuditTeamRole Role)>(members.Count);
        foreach (var member in members)
        {
            if (!AuditTeamRoleParser.TryParse(member.Role, out var role))
            {
                return Task.FromResult(ReplaceAuditTeamResult.Failure(
                    ReplaceAuditTeamError.InvalidRole, $"Rol de equipo no soportado: '{member.Role}'."));
            }

            parsedMembers.Add((member.UserId, role));
        }

        if (parsedMembers.Count(m => m.Role == AuditTeamRole.Lead) > 1)
        {
            return Task.FromResult(ReplaceAuditTeamResult.Failure(
                ReplaceAuditTeamError.MultipleLeads, "El equipo auditor admite como máximo un LEAD."));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command.AuditId, parsedMembers, ct),
            cancellationToken);
    }

    private async Task<ReplaceAuditTeamResult> ExecuteAsync(
        Guid tenantId, Guid auditId, IReadOnlyCollection<(Guid UserId, AuditTeamRole Role)> members,
        CancellationToken ct)
    {
        var audit = await auditRepository.GetByIdAsync(tenantId, auditId, ct);
        if (audit is null)
        {
            return ReplaceAuditTeamResult.Failure(ReplaceAuditTeamError.NotFound);
        }

        if (!audit.IsEditable)
        {
            return ReplaceAuditTeamResult.Failure(ReplaceAuditTeamError.NotEditable);
        }

        foreach (var member in members)
        {
            if (await userRepository.GetByIdAsync(tenantId, member.UserId, ct) is null)
            {
                return ReplaceAuditTeamResult.Failure(
                    ReplaceAuditTeamError.UserNotFound, $"Usuario no encontrado: {member.UserId}.");
            }
        }

        audit.ReplaceTeam(members, currentUser.UserId);

        return ReplaceAuditTeamResult.Success();
    }
}
