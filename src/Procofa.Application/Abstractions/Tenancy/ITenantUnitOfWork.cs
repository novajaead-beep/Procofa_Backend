namespace Procofa.Application.Abstractions.Tenancy;

/// <summary>
/// Unidad de trabajo tenant-scoped (Instrucción 03, sección 26-28; sección I
/// del baseline). Puerto puro de Application: la firma no expone EF Core,
/// Npgsql, ni ningún tipo de Infrastructure (sección 24) — la implementación
/// concreta en <c>Procofa.Infrastructure.Tenancy</c> es la que sabe que por
/// debajo hay un <c>ProcofaDbContext</c>.
/// </summary>
/// <remarks>
/// <para>
/// Contrato que la implementación de Infrastructure DEBE cumplir (decisión
/// definitiva, turno 2 del baseline — corrección 4): abre la transacción y
/// ejecuta <c>SELECT set_config('app.tenant_id', tenantId, true)</c> a través
/// del <b>mismo</b> <c>ProcofaDbContext</c> scoped que luego usará el
/// <paramref name="operation" /> delegado — nunca una <c>NpgsqlConnection</c>
/// externa ni un segundo <c>DbContext</c> vía <c>UseTransaction(...)</c>. En
/// la práctica esto se logra registrando <c>ProcofaDbContext</c> con
/// lifetime <i>scoped</i> en DI: cualquier repositorio/handler que la misma
/// request/job resuelva vía constructor injection recibe la misma instancia
/// que <see cref="ITenantUnitOfWork"/> usó para el <c>set_config</c> — sin
/// necesidad de pasar el <c>DbContext</c> explícitamente por la firma del
/// delegado, que es justamente lo que mantiene este puerto libre de EF Core.
/// </para>
/// <para>
/// <c>ExecuteReadAsync</c> NO es opcional para lecturas tenant-scoped — las
/// policies RLS no distinguen lectura de escritura (sección 27: "toda query
/// tenant-scoped: BEGIN READ ONLY; SET LOCAL tenant; query; COMMIT").
/// </para>
/// <para>
/// Fail-closed por diseño: si algún caller omitiera pasar por este puerto (o
/// una implementación futura omitiera el <c>set_config</c>), la policy RLS
/// física (<c>tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid</c>)
/// hace que la query devuelva 0 filas — nunca datos de otro tenant.
/// </para>
/// </remarks>
public interface ITenantUnitOfWork
{
    /// <summary>
    /// Ejecuta <paramref name="operation" /> dentro de una transacción de
    /// solo lectura con el tenant efectivo aplicado vía <c>SET LOCAL</c>.
    /// </summary>
    Task<T> ExecuteReadAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta <paramref name="operation" /> dentro de una transacción de
    /// escritura con el tenant efectivo aplicado vía <c>SET LOCAL</c>,
    /// llama <c>SaveChanges</c> y hace <c>COMMIT</c>; <c>ROLLBACK</c> ante
    /// cualquier excepción (incluyendo <c>DbUpdateConcurrencyException</c>
    /// por <c>lock_version</c>, que debe propagarse sin ser tragada aquí).
    /// </summary>
    Task<T> ExecuteWriteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
