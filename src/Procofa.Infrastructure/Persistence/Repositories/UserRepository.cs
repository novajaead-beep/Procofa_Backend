using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Identity;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación de <see cref="IUserRepository"/> (Instrucción 04) sobre
/// <see cref="ProcofaDbContext"/>. Deliberadamente acoplada a las
/// necesidades exactas de Auth — no un repositorio genérico. Toda query
/// asume que ya corre dentro de la transacción tenant-scoped abierta por
/// <c>ITenantUnitOfWork</c> (mismo <see cref="ProcofaDbContext"/> scoped,
/// con <c>SET LOCAL app.tenant_id</c> ya aplicado) — RLS filtra por tenant a
/// nivel de BD; el filtro explícito <c>TenantId == tenantId</c> en
/// <see cref="FindByNormalizedEmailAsync"/> es defensa en profundidad, no el
/// único mecanismo de aislamiento.
/// </summary>
public sealed class UserRepository(ProcofaDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByNormalizedEmailAsync(
        Guid tenantId, string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId && u.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetRoleCodesAsync(
        IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Code)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsWithRoleAsync(Guid tenantId, string roleCode, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Code == roleCode, cancellationToken);
        if (role is null)
        {
            return false;
        }

        return await dbContext.Users
            .Where(u => u.TenantId == tenantId)
            .SelectMany(u => u.Roles)
            .AnyAsync(userRole => userRole.RoleId == role.Id, cancellationToken);
    }
}
