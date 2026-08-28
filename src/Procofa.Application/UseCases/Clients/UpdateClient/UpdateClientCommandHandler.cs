using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Clients.ValueObjects;

namespace Procofa.Application.UseCases.Clients.UpdateClient;

public sealed class UpdateClientCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientRepository clientRepository,
    IProgramRepository programRepository)
{
    public Task<UpdateClientResult> HandleAsync(UpdateClientCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.LegalName))
        {
            return Task.FromResult(
                UpdateClientResult.Failure(UpdateClientError.ValidationFailed, "legalName es obligatorio."));
        }

        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(ct => ExecuteAsync(tenantId, command, ct), cancellationToken);
    }

    private async Task<UpdateClientResult> ExecuteAsync(
        Guid tenantId, UpdateClientCommand command, CancellationToken ct)
    {
        var client = await clientRepository.GetByIdAsync(tenantId, command.ClientId, ct);
        if (client is null)
        {
            return UpdateClientResult.Failure(UpdateClientError.NotFound);
        }

        if (!string.IsNullOrWhiteSpace(command.TaxId) &&
            await clientRepository.ExistsByTaxIdAsync(tenantId, command.TaxId, client.Id, ct))
        {
            return UpdateClientResult.Failure(
                UpdateClientError.TaxIdAlreadyExists, "Ya existe otro cliente con ese tax_id en el tenant actual.");
        }

        client.UpdateDetails(
            command.LegalName!, command.TradeName, command.TaxId, command.Industry, command.CompanyType,
            command.Notes);

        if (command.ProgramCodes is not null)
        {
            var distinctCodes = command.ProgramCodes.Distinct().ToArray();
            var resolvedPrograms = await programRepository.FindManyByCodesAsync(distinctCodes, ct);
            if (resolvedPrograms.Count != distinctCodes.Length)
            {
                var missing = distinctCodes.Except(resolvedPrograms.Select(p => p.Code));
                return UpdateClientResult.Failure(
                    UpdateClientError.ProgramNotFound,
                    $"Programa(s) no encontrados en el catálogo: {string.Join(", ", missing)}.");
            }

            client.ReplacePrograms(
                resolvedPrograms.Select(p => new ClientProgram(tenantId, client.Id, p.Id)));
        }

        return UpdateClientResult.Success();
    }
}
