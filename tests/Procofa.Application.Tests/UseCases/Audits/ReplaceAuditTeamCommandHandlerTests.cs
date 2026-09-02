using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Audits.ReplaceAuditTeam;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Identity;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.UseCases.Audits;

public sealed class ReplaceAuditTeamCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static Audit CreateSeedAudit() => new(
        Guid.NewGuid(), TenantId, "AUD-SEED-0002", Guid.NewGuid(), Guid.NewGuid(), null,
        InMemoryAuditTypeCatalog.InternaOea.Id, InMemoryProfileCatalog.Maquila.Id, Guid.NewGuid(), "Objetivo",
        "Alcance", null, new DateOnly(2026, 3, 1), Guid.NewGuid(), ExecutionMode.Remote);

    private static User CreateUser() =>
        new(Guid.NewGuid(), TenantId, $"auditor.{Guid.NewGuid():N}@procofa-test.invalid", "hash", "Nombre", "Apellido", null);

    private static (ReplaceAuditTeamCommandHandler Handler, FakeAuditRepository Audits, Audit Audit) CreateHandler(
        params User[] users)
    {
        var audit = CreateSeedAudit();
        var audits = new FakeAuditRepository(audit);
        var handler = new ReplaceAuditTeamCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits, new FakeUserRepository(users),
            new FakeCurrentUser(AdminId, "ADMIN"));

        return (handler, audits, audit);
    }

    [Fact]
    public async Task ReplaceAuditTeam_LeadYSupportValidos_ReemplazaElEquipo()
    {
        var lead = CreateUser();
        var support = CreateUser();
        var (handler, _, audit) = CreateHandler(lead, support);

        var result = await handler.HandleAsync(
            new ReplaceAuditTeamCommand(
                audit.Id,
                [new ReplaceAuditTeamMemberInput(lead.Id, "LEAD"), new ReplaceAuditTeamMemberInput(support.Id, "SUPPORT")]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, audit.Team.Count);
        Assert.Contains(audit.Team, m => m.UserId == lead.Id && m.AuditRole == AuditTeamRole.Lead);
    }

    [Fact]
    public async Task ReplaceAuditTeam_UsuarioInexistente_Falla()
    {
        var (handler, _, audit) = CreateHandler();

        var result = await handler.HandleAsync(
            new ReplaceAuditTeamCommand(audit.Id, [new ReplaceAuditTeamMemberInput(Guid.NewGuid(), "LEAD")]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditTeamError.UserNotFound, result.Error);
        Assert.Empty(audit.Team);
    }

    [Fact]
    public async Task ReplaceAuditTeam_UsuarioDuplicado_Falla()
    {
        var lead = CreateUser();
        var (handler, _, audit) = CreateHandler(lead);

        var result = await handler.HandleAsync(
            new ReplaceAuditTeamCommand(
                audit.Id,
                [new ReplaceAuditTeamMemberInput(lead.Id, "LEAD"), new ReplaceAuditTeamMemberInput(lead.Id, "SUPPORT")]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditTeamError.DuplicateUser, result.Error);
        Assert.Empty(audit.Team);
    }

    [Fact]
    public async Task ReplaceAuditTeam_SoloSupportSinLeadTodavia_PermiteConstruccionPorEtapas()
    {
        // No existe todavía un caso de uso de "planificación completa" al que atar la exigencia de
        // un LEAD — el reemplazo debe admitir un estado intermedio legítimo mientras se arma el
        // equipo (ej. asignar apoyos antes de confirmar quién lidera).
        var support = CreateUser();
        var (handler, _, audit) = CreateHandler(support);

        var result = await handler.HandleAsync(
            new ReplaceAuditTeamCommand(audit.Id, [new ReplaceAuditTeamMemberInput(support.Id, "SUPPORT")]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var member = Assert.Single(audit.Team);
        Assert.Equal(AuditTeamRole.Support, member.AuditRole);
    }

    [Fact]
    public async Task ReplaceAuditTeam_DosLead_Falla()
    {
        var leadA = CreateUser();
        var leadB = CreateUser();
        var (handler, _, audit) = CreateHandler(leadA, leadB);

        var result = await handler.HandleAsync(
            new ReplaceAuditTeamCommand(
                audit.Id,
                [new ReplaceAuditTeamMemberInput(leadA.Id, "LEAD"), new ReplaceAuditTeamMemberInput(leadB.Id, "LEAD")]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ReplaceAuditTeamError.MultipleLeads, result.Error);
        Assert.Empty(audit.Team);
    }

    [Fact]
    public async Task ReplaceAuditTeam_ColeccionVacia_LimpiaElEquipo()
    {
        var lead = CreateUser();
        var (handler, _, audit) = CreateHandler(lead);
        await handler.HandleAsync(
            new ReplaceAuditTeamCommand(audit.Id, [new ReplaceAuditTeamMemberInput(lead.Id, "LEAD")]),
            CancellationToken.None);

        var result = await handler.HandleAsync(
            new ReplaceAuditTeamCommand(audit.Id, []), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(audit.Team);
    }
}
