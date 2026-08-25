using Npgsql;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests;

/// <summary>
/// Prueba las dos funciones PL/pgSQL de inmutabilidad del baseline V2.1
/// (Instrucción 03, sección D — <c>prevent_audit_log_mutation()</c> y
/// <c>prevent_final_report_mutation()</c>):
///
/// <c>audit_logs</c> es append-only INCONDICIONAL — ningún UPDATE/DELETE está
/// permitido nunca, sin importar el valor de las columnas. La bitácora de
/// auditoría pierde su valor probatorio si puede alterarse después de escrita.
///
/// <c>audit_reports</c> es condicionalmente inmutable — solo cuando
/// <c>status = 'FINAL'</c> (un reporte en DRAFT sí puede editarse/eliminarse;
/// una vez FINAL es un documento entregado y jamás se reescribe: se genera
/// una nueva versión vía <c>version_number</c>).
///
/// Los dos tests de <c>audit_logs</c> corren deliberadamente como
/// SUPERUSUARIO, no como <c>procofa_app</c>: el ACL de <c>procofa_app</c>
/// sobre <c>audit_logs</c> es SOLO SELECT+INSERT (ver
/// <c>SchemaBootstrapTests.Bootstrap_ProcofaApp_NoTienePrivilegioUpdateNiDeleteSobreAuditLogs</c>)
/// — con <c>procofa_app</c> el UPDATE/DELETE fallaría por PERMISSION DENIED
/// (42501) ANTES de que el trigger siquiera se ejecute, lo que probaría el
/// ACL, no la función de inmutabilidad. El superusuario SÍ tiene el
/// privilegio de tabla (los triggers SIEMPRE se ejecutan pase lo que pase
/// con RLS/ACL de rol; solo RLS se salta para un superusuario real), así que
/// la única barrera que puede detener el UPDATE/DELETE es el propio
/// trigger — exactamente lo que estos dos tests validan.
///
/// El test de <c>audit_reports</c> sí corre como <c>procofa_app</c> (que
/// tiene ACL completo sobre esa tabla) para reflejar cómo la aplicación real
/// ejercería este trigger.
///
/// NO ejecutados por Claude en este sandbox (Docker inalcanzable) — ver
/// sección J/L del reporte de Instrucción 03.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class AppendOnlyAndImmutabilityTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task AuditLogs_Update_SiempreLanzaExcepcion_AunComoSuperusuario()
    {
        var tenantId = await fixture.CreateTenantAsync("append-upd");
        var logId = await fixture.CreateAuditLogAsync(tenantId, "clients", "CREATE");

        await using var connection = await fixture.OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE public.audit_logs SET action = 'UPDATE' WHERE id = @id;";
        command.Parameters.AddWithValue("id", logId);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Contains("append-only", exception.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuditLogs_Delete_SiempreLanzaExcepcion_AunComoSuperusuario()
    {
        var tenantId = await fixture.CreateTenantAsync("append-del");
        var logId = await fixture.CreateAuditLogAsync(tenantId, "clients", "CREATE");

        await using var connection = await fixture.OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM public.audit_logs WHERE id = @id;";
        command.Parameters.AddWithValue("id", logId);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Contains("append-only", exception.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuditReports_Update_ConStatusFinal_LanzaExcepcion()
    {
        var tenantId = await fixture.CreateTenantAsync("final-report");
        var userId = await fixture.CreateUserAsync(tenantId, "auditor");
        var auditData = await fixture.CreateMinimalAuditAsync(tenantId, userId, "final-report");
        var reportId = await fixture.CreateAuditReportAsync(tenantId, auditData.AuditId, userId, status: "FINAL");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetLocalTenantAsync(connection, tenantId);

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE public.audit_reports SET sha256_hex = @hash WHERE id = @id;";
        command.Parameters.AddWithValue("id", reportId);
        command.Parameters.AddWithValue("hash", new string('a', 64));

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Contains("inmutable", exception.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SetLocalTenantAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', @tenantId, true);";
        command.Parameters.AddWithValue("tenantId", tenantId.ToString());
        await command.ExecuteNonQueryAsync();
    }
}
