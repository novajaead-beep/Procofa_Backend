using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Clients;
using Procofa.Domain.Entities.Clients.ValueObjects;

namespace Procofa.Application.UseCases.Clients.CreateClient;

/// <summary>Caso de uso <c>POST /api/clients</c>. Persiste <c>Client</c> + <c>ClientPrograms</c> en
/// UNA sola transacción tenant-scoped.</summary>
public sealed class CreateClientCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientRepository clientRepository,
    IProgramRepository programRepository)
{
    public Task<CreateClientResult> HandleAsync(CreateClientCommand command, CancellationToken cancellationToken)
    {
        var validationError = Validate(command, out var programCodes);
        if (validationError is not null)
        {
            return Task.FromResult(CreateClientResult.Failure(CreateClientError.ValidationFailed, validationError));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, programCodes!, ct), cancellationToken);
    }

    private async Task<CreateClientResult> ExecuteAsync(
        Guid tenantId, CreateClientCommand command, IReadOnlyCollection<string> programCodes, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(command.TaxId) &&
            await clientRepository.ExistsByTaxIdAsync(tenantId, command.TaxId, excludeClientId: null, ct))
        {
            return CreateClientResult.Failure(
                CreateClientError.TaxIdAlreadyExists, "Ya existe un cliente con ese tax_id en el tenant actual.");
        }

        var resolvedPrograms = await programRepository.FindManyByCodesAsync(programCodes, ct);
        if (resolvedPrograms.Count != programCodes.Count)
        {
            var missing = programCodes.Except(resolvedPrograms.Select(p => p.Code));
            return CreateClientResult.Failure(
                CreateClientError.ProgramNotFound,
                $"Programa(s) no encontrados en el catálogo: {string.Join(", ", missing)}.");
        }

        var client = new Client(
            Guid.NewGuid(), tenantId, command.LegalName!, command.TradeName, command.TaxId, command.Industry,
            command.CompanyType, command.Notes);

        client.ReplacePrograms(
            resolvedPrograms.Select(p => new ClientProgram(tenantId, client.Id, p.Id)));

        await clientRepository.AddAsync(client, ct);

        return CreateClientResult.Success(client.Id);
    }

    private static string? Validate(CreateClientCommand command, out IReadOnlyCollection<string>? programCodes)
    {
        programCodes = null;

        if (string.IsNullOrWhiteSpace(command.LegalName))
        {
            return "legalName es obligatorio.";
        }

        programCodes = (command.ProgramCodes ?? []).Distinct().ToArray();
        return null;
    }
}
