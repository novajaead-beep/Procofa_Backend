using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Tests.TestDoubles;

/// <summary>
/// Fakes deterministas para los tests de Auth. No se usa ninguna librería de mocking —
/// Procofa.Application.Tests solo referencia xunit (Directory.Packages.props, política conservadora
/// de Foundation) — así que cada puerto tiene aquí una implementación en memoria mínima, suficiente
/// para observar el comportamiento de los handlers sin BD real. </summary>
internal static class InMemoryRoleCatalog
{
    public static readonly Role Admin = new(Guid.NewGuid(), "ADMIN", "Administrador", null);
    public static readonly Role AuditorLider = new(Guid.NewGuid(), "AUDITOR_LIDER", "Auditor Líder", null);
    public static readonly Role AuditorApoyo = new(Guid.NewGuid(), "AUDITOR_APOYO", "Auditor Apoyo", null);
    public static readonly Role Cliente = new(Guid.NewGuid(), "CLIENTE", "Cliente", null);
    public static readonly Role Consultor = new(Guid.NewGuid(), "CONSULTOR", "Consultor", null);

    // catálogo completo de los 5 roles válidos del módulo de usuarios.
    public static readonly IReadOnlyList<Role> All = [Admin, AuditorLider, AuditorApoyo, Cliente, Consultor];
}

internal sealed class FakeTenantContext(Guid tenantId) : ITenantContext
{
    public Guid TenantId { get; } = tenantId;
}

/// <summary>Ejecuta el delegado directamente, sin transacción real — suficiente para tests de orquestación de Application.</summary>
internal sealed class FakeTenantUnitOfWork : ITenantUnitOfWork
{
    public Task<T> ExecuteReadAsync<T>(
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        operation(cancellationToken);

    public Task<T> ExecuteWriteAsync<T>(
        Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        operation(cancellationToken);
}

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public FakeUserRepository(params User[] seedUsers) => _users.AddRange(seedUsers);

    public IReadOnlyList<User> Users => _users;

    public Task<User?> FindByNormalizedEmailAsync(
        Guid tenantId, string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.TenantId == tenantId && u.NormalizedEmail == normalizedEmail));

    public Task<IReadOnlyCollection<string>> GetRoleCodesAsync(
        IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<string>>(
            InMemoryRoleCatalog.All.Where(r => roleIds.Contains(r.Id)).Select(r => r.Code).ToArray());

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsWithRoleAsync(Guid tenantId, string roleCode, CancellationToken cancellationToken)
    {
        var role = InMemoryRoleCatalog.All.FirstOrDefault(r => r.Code == roleCode);
        var exists = role is not null &&
            _users.Any(u => u.TenantId == tenantId && u.Roles.Any(ur => ur.RoleId == role.Id));
        return Task.FromResult(exists);
    }

    // ---- gestión de usuarios ----

    public Task<bool> ExistsByNormalizedEmailAsync(
        Guid tenantId, string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(_users.Any(u => u.TenantId == tenantId && u.NormalizedEmail == normalizedEmail));

    public Task<User?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_users.FirstOrDefault(u => u.TenantId == tenantId && u.Id == userId));

    public Task<UserListPageResult> ListAsync(
        Guid tenantId, string? search, bool? isActive, string? roleCode, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        IEnumerable<User> query = _users.Where(u => u.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            var role = InMemoryRoleCatalog.All.FirstOrDefault(r => r.Code == roleCode);
            query = role is null ? [] : query.Where(u => u.Roles.Any(ur => ur.RoleId == role.Id));
        }

        var materialized = query.OrderBy(u => u.Email).ToList();
        var items = materialized
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListRow(
                u.Id, u.Email, u.FirstName, u.LastName, u.Phone, u.IsActive, u.MustChangePassword,
                u.Roles
                    .Select(ur => InMemoryRoleCatalog.All.FirstOrDefault(r => r.Id == ur.RoleId)?.Code ?? ur.RoleId.ToString())
                    .ToArray(),
                u.CreatedAtUtc))
            .ToList();

        return Task.FromResult(new UserListPageResult(items, materialized.Count));
    }
}

internal sealed class FakeRoleRepository : IRoleRepository
{
    public Task<Role?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(InMemoryRoleCatalog.All.FirstOrDefault(r => r.Code == code));

    public Task<IReadOnlyCollection<Role>> FindManyByCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Role>>(
            InMemoryRoleCatalog.All.Where(r => codes.Contains(r.Code)).ToArray());
}

internal sealed class FakeAccessLogRepository : IAccessLogRepository
{
    public List<AccessLog> Logged { get; } = [];

    public Task AddAsync(AccessLog accessLog, CancellationToken cancellationToken)
    {
        Logged.Add(accessLog);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Added { get; } = [];

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        Added.Add(refreshToken);
        return Task.CompletedTask;
    }
}

internal sealed class FakePasswordHasher(PasswordVerificationResult result) : IPasswordHasher
{
    public string HashPassword(string password) => $"hashed:{password}";

    public PasswordVerificationResult VerifyPassword(string passwordHash, string providedPassword) => result;
}

internal sealed class FakeAccessTokenGenerator : IAccessTokenGenerator
{
    public IReadOnlyCollection<string>? LastRoleCodes { get; private set; }

    public AccessToken GenerateAccessToken(
        Guid userId, Guid tenantId, string email, IReadOnlyCollection<string> roleCodes)
    {
        LastRoleCodes = roleCodes;
        return new AccessToken("fake-access-token", new DateTime(2026, 1, 1, 0, 15, 0, DateTimeKind.Utc));
    }
}

internal sealed class FakeRefreshTokenFactory : IRefreshTokenFactory
{
    public GeneratedRefreshToken Create() => new("fake-raw-refresh-token", "fake-refresh-token-hash");
}

internal sealed class FakeAuthPolicyOptions : IAuthPolicyOptions
{
    public int MaxFailedLoginAttempts { get; init; } = 5;
    public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(30);
}

internal sealed class FakeDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = utcNow;
}
