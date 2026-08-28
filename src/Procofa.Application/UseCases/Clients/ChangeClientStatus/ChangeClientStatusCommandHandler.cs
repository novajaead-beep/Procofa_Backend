using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Application.UseCases.Clients.ChangeClientStatus;

/// <summary>Caso de uso <c>PATCH /api/clients/{clientId}/status</c>. Nunca hace hard delete — solo
/// activa/desactiva.</summary>
public sealed class ChangeClientStatusCommandHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IClientRepository clientRepository)
{
    public Task<ChangeClientStatusResult> HandleAsync(
        ChangeClientStatusCommand command, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId;

        return unitOfWork.ExecuteWriteAsync(
            async ct =>
            {
                var client = await clientRepository.GetByIdAsync(tenantId, command.ClientId, ct);
                if (client is null)
                {
                    return ChangeClientStatusResult.Failure(ChangeClientStatusError.NotFound);
                }

                if (command.IsActive)
                {
                    client.Activate();
                }
                else
                {
                    client.Deactivate();
                }

                return ChangeClientStatusResult.Success();
            },
            cancellationToken);
    }
}
