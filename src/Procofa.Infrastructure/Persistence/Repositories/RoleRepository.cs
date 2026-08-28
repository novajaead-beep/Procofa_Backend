using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Identity;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación de <see cref="IRoleRepository"/>. El catálogo <c>roles</c> es global, sin
/// <c>tenant_id</c>, sin RLS — no hay filtro de tenant que aplicar aquí (fiel al baseline: ver <see
/// cref="Role"/>). </summary>
public sealed class RoleRepository(ProcofaDbContext dbContext) : IRoleRepository
{
    public Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Roles.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);

    public async Task<IReadOnlyCollection<Role>> FindManyByCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        if (codes.Count == 0)
        {
            return [];
        }

        return await dbContext.Roles.Where(r => codes.Contains(r.Code)).ToListAsync(cancellationToken);
    }
}
