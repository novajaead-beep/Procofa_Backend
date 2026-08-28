using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Identity;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementación de <see cref="IUserRepository"/> sobre <see cref="ProcofaDbContext"/>.
/// Deliberadamente acoplada a las necesidades exactas de Auth — no un repositorio genérico. Toda
/// query asume que ya corre dentro de la transacción tenant-scoped abierta por
/// <c>ITenantUnitOfWork</c> (mismo <see cref="ProcofaDbContext"/> scoped, con <c>SET LOCAL
/// app.tenant_id</c> ya aplicado) — RLS filtra por tenant a nivel de BD; el filtro explícito
/// <c>TenantId == tenantId</c> en <see cref="FindByNormalizedEmailAsync"/> es defensa en
/// profundidad, no el único mecanismo de aislamiento. </summary>
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

    public Task<bool> ExistsByNormalizedEmailAsync(
        Guid tenantId, string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(
            u => u.TenantId == tenantId && u.NormalizedEmail == normalizedEmail, cancellationToken);

    // AsSplitQuery() al cargar Roles + ClientAccess simultáneamente sobre un mismo User — evita el
    // warning de EF por múltiples colecciones en una sola consulta (cartesian explosion).
    public Task<User?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users
            .Include(u => u.Roles)
            .Include(u => u.ClientAccess)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);

    public async Task<UserListPageResult> ListAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        string? roleCode,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users.Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, pattern) ||
                EF.Functions.ILike(u.FirstName, pattern) ||
                EF.Functions.ILike(u.LastName, pattern));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            query = query.Where(u => u.Roles.Any(ur =>
                dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Code == roleCode)));
        }

        var total = await query.CountAsync(cancellationToken);

        var pageRows = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Phone,
                u.IsActive,
                u.MustChangePassword,
                u.CreatedAtUtc,
                RoleIds = u.Roles.Select(r => r.RoleId).ToList(),
            })
            .ToListAsync(cancellationToken);

        var allRoleIds = pageRows.SelectMany(u => u.RoleIds).Distinct().ToArray();
        var roleCodesById = await dbContext.Roles
            .Where(r => allRoleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Code, cancellationToken);

        var items = pageRows
            .Select(u => new UserListRow(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Phone,
                u.IsActive,
                u.MustChangePassword,
                u.RoleIds.Select(id => roleCodesById.GetValueOrDefault(id, id.ToString())).ToArray(),
                u.CreatedAtUtc))
            .ToList();

        return new UserListPageResult(items, total);
    }
}
