using Procofa.Application.Tests.TestDoubles;
using Procofa.Application.UseCases.Audits.UpdateAudit;
using Procofa.Domain.Entities.Audits;
using Procofa.Domain.Entities.Checklists;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Enums;

namespace Procofa.Application.Tests.UseCases.Audits;

public sealed class UpdateAuditCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private sealed record Fixture(
        UpdateAuditCommandHandler Handler, FakeAuditRepository Audits, Audit Audit,
        FakeAuditChecklistRepository AuditChecklists, FakeChecklistRepository Checklists,
        FakeChecklistVersionRepository Versions);

    private static Fixture CreateHandler(bool started = false)
    {
        var client = new Client(Guid.NewGuid(), TenantId, "Cliente", null, null, null, null, null);
        var company = new AuditedCompany(
            Guid.NewGuid(), TenantId, client.Id, null, "Planta", null, null, null, null, false);
        var site = new CompanySite(
            Guid.NewGuid(), TenantId, company.Id, "Sede", "Calle 1", null, null, null, null, "México", null, null);

        var audit = new Audit(
            Guid.NewGuid(), TenantId, "AUD-SEED-0005", client.Id, company.Id, site.Id,
            InMemoryAuditTypeCatalog.InternaOea.Id, InMemoryProfileCatalog.Maquila.Id, Guid.NewGuid(), "Objetivo",
            "Alcance", null, new DateOnly(2026, 3, 1), Guid.NewGuid(), ExecutionMode.Onsite);
        audit.ReplacePrograms([InMemoryProgramCatalog.Oea.Id]);

        if (started)
        {
            typeof(Audit).GetProperty(nameof(Audit.StartedAtUtc))!.SetValue(audit, DateTime.UtcNow);
        }

        var audits = new FakeAuditRepository(audit);
        var checklists = new FakeChecklistRepository();
        var versions = new FakeChecklistVersionRepository();
        var auditChecklists = new FakeAuditChecklistRepository(checklists, versions);
        var handler = new UpdateAuditCommandHandler(
            new FakeTenantContext(TenantId), new FakeTenantUnitOfWork(), audits,
            new FakeAuditedCompanyRepository(company), new FakeCompanySiteRepository(site),
            new FakeAuditTypeRepository(), new FakeProfileRepository(), auditChecklists);

        return new Fixture(handler, audits, audit, auditChecklists, checklists, versions);
    }

    private static async Task SeedAssignedChecklistAsync(Fixture fixture, Guid profileId, Guid? auditTypeId)
    {
        var checklist = new Checklist(
            Guid.NewGuid(), TenantId, InMemoryProgramCatalog.Oea.Id, profileId, auditTypeId, "Checklist Asignado",
            null, Guid.NewGuid());
        var version = new ChecklistVersion(Guid.NewGuid(), TenantId, checklist.Id, 1, Guid.NewGuid());
        version.Publish(DateTime.UtcNow);

        await fixture.Checklists.AddAsync(checklist, CancellationToken.None);
        await fixture.Versions.CreateNextVersionAsync(TenantId, checklist.Id, _ => version, CancellationToken.None);
        await fixture.AuditChecklists.ReplaceAsync(
            TenantId, fixture.Audit.Id, [new AuditChecklist(Guid.NewGuid(), TenantId, fixture.Audit.Id, version.Id)],
            CancellationToken.None);
    }

    [Fact]
    public async Task UpdateAudit_Editable_ActualizaLosDatos()
    {
        var fixture = CreateHandler();

        var result = await fixture.Handler.HandleAsync(
            new UpdateAuditCommand(
                fixture.Audit.Id, fixture.Audit.AuditedCompanyId, fixture.Audit.CompanySiteId,
                fixture.Audit.AuditTypeId, fixture.Audit.ProfileId, "Objetivo actualizado", "Alcance actualizado",
                null, new DateOnly(2026, 4, 1), "REMOTE"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Objetivo actualizado", fixture.Audit.Objective);
        Assert.Equal(ExecutionMode.Remote, fixture.Audit.ExecutionMode);
    }

    [Fact]
    public async Task UpdateAudit_YaIniciada_Falla()
    {
        var fixture = CreateHandler(started: true);

        var result = await fixture.Handler.HandleAsync(
            new UpdateAuditCommand(
                fixture.Audit.Id, fixture.Audit.AuditedCompanyId, fixture.Audit.CompanySiteId,
                fixture.Audit.AuditTypeId, fixture.Audit.ProfileId, "Objetivo actualizado", "Alcance actualizado",
                null, new DateOnly(2026, 4, 1), "ONSITE"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateAuditError.NotEditable, result.Error);
    }

    [Fact]
    public async Task UpdateAudit_CambioCompatibleConChecklistAsignado_Ok()
    {
        var fixture = CreateHandler();
        await SeedAssignedChecklistAsync(fixture, fixture.Audit.ProfileId, fixture.Audit.AuditTypeId);

        var result = await fixture.Handler.HandleAsync(
            new UpdateAuditCommand(
                fixture.Audit.Id, fixture.Audit.AuditedCompanyId, fixture.Audit.CompanySiteId,
                fixture.Audit.AuditTypeId, fixture.Audit.ProfileId, "Objetivo actualizado", "Alcance actualizado",
                null, new DateOnly(2026, 4, 1), "ONSITE"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdateAudit_CambioDeProfileInvalidaChecklistAsignado_Falla()
    {
        var fixture = CreateHandler();
        await SeedAssignedChecklistAsync(fixture, fixture.Audit.ProfileId, fixture.Audit.AuditTypeId);

        var result = await fixture.Handler.HandleAsync(
            new UpdateAuditCommand(
                fixture.Audit.Id, fixture.Audit.AuditedCompanyId, fixture.Audit.CompanySiteId,
                fixture.Audit.AuditTypeId, InMemoryProfileCatalog.Transportista.Id, "Objetivo", "Alcance", null,
                new DateOnly(2026, 4, 1), "ONSITE"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateAuditError.ChecklistIncompatible, result.Error);
        Assert.Equal(InMemoryProfileCatalog.Maquila.Id, fixture.Audit.ProfileId);
    }

    [Fact]
    public async Task UpdateAudit_CambioDeAuditTypeInvalidaChecklistAsignado_Falla()
    {
        var fixture = CreateHandler();
        await SeedAssignedChecklistAsync(fixture, fixture.Audit.ProfileId, fixture.Audit.AuditTypeId);

        var result = await fixture.Handler.HandleAsync(
            new UpdateAuditCommand(
                fixture.Audit.Id, fixture.Audit.AuditedCompanyId, fixture.Audit.CompanySiteId,
                InMemoryAuditTypeCatalog.InternaCtpat.Id, fixture.Audit.ProfileId, "Objetivo", "Alcance", null,
                new DateOnly(2026, 4, 1), "ONSITE"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateAuditError.ChecklistIncompatible, result.Error);
        Assert.Equal(InMemoryAuditTypeCatalog.InternaOea.Id, fixture.Audit.AuditTypeId);
    }

    [Fact]
    public async Task UpdateAudit_OnsiteSinSitio_Falla()
    {
        var fixture = CreateHandler();

        var result = await fixture.Handler.HandleAsync(
            new UpdateAuditCommand(
                fixture.Audit.Id, fixture.Audit.AuditedCompanyId, null, fixture.Audit.AuditTypeId,
                fixture.Audit.ProfileId, "Objetivo", "Alcance", null, new DateOnly(2026, 4, 1), "ONSITE"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateAuditError.ValidationFailed, result.Error);
    }
}
