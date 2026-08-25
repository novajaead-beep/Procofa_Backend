using Npgsql;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests;

/// <summary>
/// Prueba <c>validate_audit_before_close()</c> (Instrucción 03, sección D —
/// una de las 6 funciones PL/pgSQL del baseline V2.1), disparada por
/// <c>trg_audits_validate_close BEFORE INSERT OR UPDATE OF status_id</c>:
/// una auditoría NO puede pasar a <c>CERRADA</c> si (1) existe algún
/// <c>audit_criteria</c> obligatorio (<c>is_mandatory_snapshot = TRUE</c>)
/// sin evaluar (<c>compliance_status_id IS NULL</c>), o (2) sin que el
/// auditor líder la haya validado (<c>validated_by_user_id</c> Y
/// <c>validated_at_utc</c> ambos no nulos). Si ambas condiciones se
/// cumplen, el trigger además AUTO-establece <c>closed_at_utc</c> cuando
/// llega NULL — el cierre nunca depende de que la aplicación calcule ese
/// timestamp.
///
/// Corre como <c>procofa_app</c> con <c>SET LOCAL</c> del tenant — refleja
/// exactamente cómo la aplicación real dispararía este trigger al cerrar
/// una auditoría (procofa_app tiene ACL completo sobre <c>audits</c>).
///
/// NO ejecutado por Claude en este sandbox (Docker inalcanzable) — ver
/// sección J/L del reporte de Instrucción 03.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class AuditCloseValidationTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task Close_ConCriterioObligatorioSinEvaluar_LanzaExcepcion()
    {
        var tenantId = await fixture.CreateTenantAsync("close-pending");
        var userId = await fixture.CreateUserAsync(tenantId, "lider");
        var auditData = await fixture.CreateMinimalAuditAsync(tenantId, userId, "close-pending");

        // Criterio obligatorio, compliance_status_id = NULL -- todavía sin
        // evaluar. El trigger debe rechazar el cierre ANTES de siquiera
        // llegar a validar validated_by_user_id/validated_at_utc.
        await fixture.CreateAuditCriterionAsync(
            tenantId,
            auditData.AuditId,
            userId,
            isMandatorySnapshot: true,
            complianceStatusCode: null,
            suffix: "close-pending");

        var closedStatusId = await fixture.GetCatalogIdByCodeAsync("audit_statuses", "CERRADA");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetLocalTenantAsync(connection, tenantId);

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE public.audits SET status_id = @statusId WHERE id = @id;";
        command.Parameters.AddWithValue("statusId", closedStatusId);
        command.Parameters.AddWithValue("id", auditData.AuditId);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Contains("sin evaluar", exception.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Close_ConTodoEvaluadoYValidadoPorLider_TieneExitoYAutoEstableceClosedAtUtc()
    {
        var tenantId = await fixture.CreateTenantAsync("close-ok");
        var leaderId = await fixture.CreateUserAsync(tenantId, "lider");
        var auditData = await fixture.CreateMinimalAuditAsync(tenantId, leaderId, "close-ok");

        // Mismo criterio obligatorio, esta vez YA evaluado (CUMPLE).
        await fixture.CreateAuditCriterionAsync(
            tenantId,
            auditData.AuditId,
            leaderId,
            isMandatorySnapshot: true,
            complianceStatusCode: "CUMPLE",
            suffix: "close-ok");

        var closedStatusId = await fixture.GetCatalogIdByCodeAsync("audit_statuses", "CERRADA");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetLocalTenantAsync(connection, tenantId);

        // status_id, validated_by_user_id y validated_at_utc en el MISMO
        // UPDATE -- el trigger es "OF status_id", así que basta con que
        // status_id esté en el SET; NEW ya refleja los tres valores nuevos
        // juntos al evaluarse las condiciones.
        await using (var updateCommand = connection.CreateCommand())
        {
            updateCommand.CommandText = """
                UPDATE public.audits
                   SET status_id = @statusId,
                       validated_by_user_id = @leaderId,
                       validated_at_utc = NOW()
                 WHERE id = @id;
                """;
            updateCommand.Parameters.AddWithValue("statusId", closedStatusId);
            updateCommand.Parameters.AddWithValue("leaderId", leaderId);
            updateCommand.Parameters.AddWithValue("id", auditData.AuditId);

            var affected = await updateCommand.ExecuteNonQueryAsync();
            Assert.Equal(1, affected);
        }

        await using var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT closed_at_utc FROM public.audits WHERE id = @id;";
        selectCommand.Parameters.AddWithValue("id", auditData.AuditId);
        var closedAtUtc = await selectCommand.ExecuteScalarAsync();

        // closed_at_utc llegó NULL al UPDATE -- el trigger debió
        // autoestablecerlo a NOW() sin que la aplicación lo haya pedido.
        Assert.NotNull(closedAtUtc);
        Assert.NotEqual(DBNull.Value, closedAtUtc);
    }

    private static async Task SetLocalTenantAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', @tenantId, true);";
        command.Parameters.AddWithValue("tenantId", tenantId.ToString());
        await command.ExecuteNonQueryAsync();
    }
}
