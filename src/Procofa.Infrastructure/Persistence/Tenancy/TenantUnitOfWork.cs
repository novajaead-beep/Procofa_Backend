using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Infrastructure.Persistence.Tenancy;

/// <summary>
/// Implementación concreta de <see cref="ITenantUnitOfWork"/> (Instrucción
/// 03, sección 26-28). Reutiliza la MISMA instancia scoped de
/// <see cref="ProcofaDbContext"/> inyectada por constructor — nunca abre una
/// <c>NpgsqlConnection</c> ni un <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
/// alternativo: el <c>set_config</c> del tenant se ejecuta a través de
/// <c>ProcofaDbContext.Database</c>, en la MISMA conexión/transacción que
/// luego usa el <c>operation</c> delegado (contrato documentado en el
/// puerto — decisión definitiva, no reinterpretable en esta implementación).
///
/// <c>ExecuteReadAsync</c> replica literalmente "BEGIN READ ONLY; SET LOCAL
/// tenant; query; COMMIT" (Instrucción 03, sección 27) como dos sentencias
/// (<c>BEGIN</c> vía EF + <c>SET TRANSACTION READ ONLY</c> como primera
/// sentencia de la transacción, regla de Postgres) porque la API de EF Core
/// para iniciar transacciones no expone un modo <c>READ ONLY</c> directo.
///
/// <c>set_config('app.tenant_id', tenantId, true)</c> con <c>is_local=true</c>
/// (equivalente a <c>SET LOCAL</c>) — el valor NO sobrevive fuera de la
/// transacción activa, ni siquiera si la conexión física vuelve a un pool.
/// Fail-closed explícito: si <see cref="ITenantContext.TenantId"/> fuera
/// <see cref="Guid.Empty"/> (tenant no resuelto), esta clase lanza antes de
/// tocar la BD — no confía únicamente en que la policy RLS física
/// (<c>NULLIF(current_setting(...), '')::uuid</c>) intercepte el caso.
/// </summary>
public sealed class TenantUnitOfWork : ITenantUnitOfWork
{
    private readonly ProcofaDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public TenantUnitOfWork(ProcofaDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task<T> ExecuteReadAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        // Debe ser la PRIMERA sentencia tras BEGIN (regla de Postgres) —
        // antes incluso del set_config del tenant.
        await _dbContext.Database
            .ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", cancellationToken)
            .ConfigureAwait(false);

        await SetLocalTenantAsync(cancellationToken).ConfigureAwait(false);

        var result = await operation(cancellationToken).ConfigureAwait(false);

        // Transacción de solo lectura: no hay SaveChanges, solo cierre limpio.
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return result;
    }

    public async Task<T> ExecuteWriteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await SetLocalTenantAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return result;
        }
        catch
        {
            // Rollback explícito antes de propagar — incluye
            // DbUpdateConcurrencyException por lock_version, que NUNCA se
            // traga aquí (contrato del puerto: ver ITenantUnitOfWork).
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task SetLocalTenantAsync(CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "ITenantContext.TenantId no fue resuelto (Guid.Empty) — no existe " +
                "operación tenant-scoped válida sin un tenant efectivo (fail-closed).");
        }

        var tenantId = _tenantContext.TenantId.ToString();

        await _dbContext.Database
            .ExecuteSqlInterpolatedAsync(
                $"SELECT set_config('app.tenant_id', {tenantId}, true)",
                cancellationToken)
            .ConfigureAwait(false);
    }
}
