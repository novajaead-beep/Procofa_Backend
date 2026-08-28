using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Clients;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación de <see cref="IClientRepository"/>. Solo lectura, filtrada SIEMPRE por tenant —
/// mismo principio de defensa en profundidad que <see
/// cref="UserRepository.FindByNormalizedEmailAsync"/>: RLS filtra a nivel de BD, el filtro
/// explícito aquí no es el único mecanismo de aislamiento. </summary>
public sealed class ClientRepository(ProcofaDbContext dbContext) : IClientRepository
{
    public async Task<IReadOnlyCollection<Client>> FindManyByIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Clients
            .Where(c => c.TenantId == tenantId && clientIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }
}
