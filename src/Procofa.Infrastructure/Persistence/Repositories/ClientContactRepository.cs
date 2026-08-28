using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Clients;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class ClientContactRepository(ProcofaDbContext dbContext) : IClientContactRepository
{
    public Task<ClientContact?> GetByIdAsync(
        Guid tenantId, Guid clientId, Guid contactId, CancellationToken cancellationToken) =>
        dbContext.ClientContacts
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.ClientId == clientId && c.Id == contactId, cancellationToken);

    public Task AddAsync(ClientContact contact, CancellationToken cancellationToken)
    {
        dbContext.ClientContacts.Add(contact);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ClientContact>> ListByClientAsync(
        Guid tenantId, Guid clientId, CancellationToken cancellationToken) =>
        await dbContext.ClientContacts
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.ClientId == clientId)
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync(cancellationToken);
}
