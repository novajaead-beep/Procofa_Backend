using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Clients;
using Procofa.Domain.Entities.Clients;

namespace Procofa.Application.Tests.TestDoubles;

/// <summary>Fakes deterministas para los tests de gestión de usuarios (Instrucción 05) — mismo criterio que <see cref="InMemoryRoleCatalog"/> y compañía en AuthTestDoubles.cs: sin librería de mocking.</summary>
internal sealed class FakeCurrentUser(Guid userId) : ICurrentUser
{
    public Guid UserId { get; } = userId;
}

internal sealed class FakeClientRepository : IClientRepository
{
    private readonly List<Client> _clients = [];

    public FakeClientRepository(params Client[] seedClients) => _clients.AddRange(seedClients);

    public Task<IReadOnlyCollection<Client>> FindManyByIdsAsync(
        Guid tenantId, IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Client>>(
            _clients.Where(c => c.TenantId == tenantId && clientIds.Contains(c.Id)).ToArray());
}
