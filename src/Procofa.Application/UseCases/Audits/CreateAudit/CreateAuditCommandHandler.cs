using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Audits;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Audits;

namespace Procofa.Application.UseCases.Audits.CreateAudit;

/// <summary>Caso de uso <c>POST /api/audits</c>. Persiste <c>Audit</c> + <c>AuditPrograms</c> en
/// UNA sola transacción tenant-scoped — el estado inicial siempre resuelve al catálogo
/// <c>audit_statuses.code = 'BORRADOR'</c>.</summary>
public sealed class CreateAuditCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IAuditRepository auditRepository,
    IClientRepository clientRepository,
    IAuditedCompanyRepository auditedCompanyRepository,
    ICompanySiteRepository companySiteRepository,
    IAuditTypeRepository auditTypeRepository,
    IProfileRepository profileRepository,
    IProgramRepository programRepository,
    IAuditStatusRepository auditStatusRepository,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider)
{
    private const string InitialStatusCode = "BORRADOR";

    public Task<CreateAuditResult> HandleAsync(CreateAuditCommand command, CancellationToken cancellationToken)
    {
        var validationError = Validate(
            command, out var programCodes, out var executionMode);
        if (validationError is not null)
        {
            return Task.FromResult(CreateAuditResult.Failure(CreateAuditError.ValidationFailed, validationError));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            ct => ExecuteAsync(tenantId, command, programCodes!, executionMode, ct), cancellationToken);
    }

    private async Task<CreateAuditResult> ExecuteAsync(
        Guid tenantId,
        CreateAuditCommand command,
        IReadOnlyCollection<string> programCodes,
        Domain.Enums.ExecutionMode executionMode,
        CancellationToken ct)
    {
        if (await clientRepository.GetByIdAsync(tenantId, command.ClientId!.Value, ct) is null)
        {
            return CreateAuditResult.Failure(CreateAuditError.ClientNotFound);
        }

        if (await auditedCompanyRepository.GetByIdAsync(
                tenantId, command.ClientId.Value, command.AuditedCompanyId!.Value, ct) is null)
        {
            return CreateAuditResult.Failure(CreateAuditError.AuditedCompanyNotFound);
        }

        if (command.CompanySiteId.HasValue &&
            await companySiteRepository.GetByIdAsync(
                tenantId, command.AuditedCompanyId.Value, command.CompanySiteId.Value, ct) is null)
        {
            return CreateAuditResult.Failure(CreateAuditError.CompanySiteNotFound);
        }

        if (await auditTypeRepository.GetByIdAsync(command.AuditTypeId!.Value, ct) is null)
        {
            return CreateAuditResult.Failure(CreateAuditError.AuditTypeNotFound);
        }

        if (await profileRepository.GetByIdAsync(command.ProfileId!.Value, ct) is null)
        {
            return CreateAuditResult.Failure(CreateAuditError.ProfileNotFound);
        }

        var status = await auditStatusRepository.FindByCodeAsync(InitialStatusCode, ct);
        if (status is null)
        {
            return CreateAuditResult.Failure(CreateAuditError.StatusNotFound);
        }

        var resolvedPrograms = await programRepository.FindManyByCodesAsync(programCodes, ct);
        if (resolvedPrograms.Count != programCodes.Count)
        {
            var missing = programCodes.Except(resolvedPrograms.Select(p => p.Code));
            return CreateAuditResult.Failure(
                CreateAuditError.ProgramNotFound,
                $"Programa(s) no encontrados en el catálogo: {string.Join(", ", missing)}.");
        }

        var folio = await GenerateUniqueFolioAsync(tenantId, ct);

        var audit = new Audit(
            Guid.NewGuid(), tenantId, folio, command.ClientId.Value, command.AuditedCompanyId.Value,
            command.CompanySiteId, command.AuditTypeId.Value, command.ProfileId.Value, status.Id,
            command.Objective!, command.Scope!, command.Methodology, command.ScheduledDate!.Value,
            currentUser.UserId, executionMode);

        if (resolvedPrograms.Count > 0)
        {
            audit.ReplacePrograms(resolvedPrograms.Select(p => p.Id).ToArray());
        }

        await auditRepository.AddAsync(audit, ct);

        return CreateAuditResult.Success(audit.Id, audit.Folio);
    }

    /// <summary>Sin esquema de numeración secuencial especificado — se optó por un folio derivado
    /// de la fecha + un componente aleatorio (<c>Guid.NewGuid</c>) para evitar colisiones sin
    /// inventar una secuencia no solicitada; el chequeo de unicidad contra <c>ExistsFolioAsync</c>
    /// cubre la probabilidad remota de colisión del componente aleatorio.</summary>
    private async Task<string> GenerateUniqueFolioAsync(Guid tenantId, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"AUD-{dateTimeProvider.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}";
            candidate = candidate.Length > 50 ? candidate[..50] : candidate;

            if (!await auditRepository.ExistsFolioAsync(tenantId, candidate, ct))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No fue posible generar un folio único para la auditoría.");
    }

    private static string? Validate(
        CreateAuditCommand command,
        out IReadOnlyCollection<string>? programCodes,
        out Domain.Enums.ExecutionMode executionMode)
    {
        programCodes = null;
        executionMode = default;

        if (command.ClientId is null)
        {
            return "clientId es obligatorio.";
        }

        if (command.AuditedCompanyId is null)
        {
            return "auditedCompanyId es obligatorio.";
        }

        if (command.AuditTypeId is null)
        {
            return "auditTypeId es obligatorio.";
        }

        if (command.ProfileId is null)
        {
            return "profileId es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.Objective))
        {
            return "objective es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(command.Scope))
        {
            return "scope es obligatorio.";
        }

        if (command.ScheduledDate is null)
        {
            return "scheduledDate es obligatorio.";
        }

        if (!ExecutionModeParser.TryParse(command.ExecutionMode, out executionMode))
        {
            return "executionMode debe ser ONSITE, REMOTE o HYBRID.";
        }

        if (ExecutionModeParser.RequiresCompanySite(executionMode) && command.CompanySiteId is null)
        {
            return $"execution_mode = {command.ExecutionMode} requiere companySiteId.";
        }

        programCodes = (command.ProgramCodes ?? []).Distinct().ToArray();
        return null;
    }
}
