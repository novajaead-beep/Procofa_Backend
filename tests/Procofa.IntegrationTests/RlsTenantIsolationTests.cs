using Npgsql;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests;

/// <summary>
/// Prueba el contrato de aislamiento multitenant vía RLS descrito en
/// <c>ITenantUnitOfWork</c> y en el baseline V2.1 sección I (Instrucción 03,
/// sección 27): fail-closed sin tenant, aislamiento real entre dos
/// tenants, <c>SET LOCAL</c> no sobrevive fuera de su transacción (ni
/// siquiera en la MISMA conexión física — el escenario relevante para
/// connection pooling), rechazo de <c>INSERT</c> con <c>tenant_id</c>
/// distinto al de la sesión (policy <c>WITH CHECK</c>), y que
/// <c>procofa_app</c> nunca es superusuario ni tiene <c>BYPASSRLS</c>.
///
/// Todas las queries de este archivo corren como <c>procofa_app</c>
/// (<see cref="PostgresBaselineFixture.OpenAppConnectionAsync"/>) — nunca
/// como superusuario — para ejercer RLS/ACL tal como los ejercería la
/// aplicación real. NO ejecutados por Claude en este sandbox (Docker
/// inalcanzable) — ver sección J/L del reporte de Instrucción 03.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class RlsTenantIsolationTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task SinTenantEnSesion_QueryTenantScoped_DevuelveCeroFilas_FailClosed()
    {
        var tenantId = await fixture.CreateTenantAsync("failclosed");
        await fixture.CreateClientAsync(tenantId, "Cliente Fail-Closed");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        // Deliberadamente SIN set_config('app.tenant_id', ...) — la policy
        // usa NULLIF(current_setting(..., true), '')::uuid, que da NULL, y
        // "tenant_id = NULL" nunca es verdadero en SQL: fail-closed por diseño.
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM public.clients;";
        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ConTenantA_SoloVeFilasDelTenantA_NuncaLasDeTenantB()
    {
        var tenantA = await fixture.CreateTenantAsync("iso-a");
        var tenantB = await fixture.CreateTenantAsync("iso-b");
        var clientA = await fixture.CreateClientAsync(tenantA, "Cliente A");
        await fixture.CreateClientAsync(tenantB, "Cliente B");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetLocalTenantAsync(connection, tenantA);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM public.clients;";
        await using var reader = await command.ExecuteReaderAsync();

        var visibleIds = new List<Guid>();
        while (await reader.ReadAsync())
        {
            visibleIds.Add(reader.GetGuid(0));
        }

        Assert.Single(visibleIds);
        Assert.Equal(clientA, visibleIds[0]);
    }

    [Fact]
    public async Task SetLocal_NoSobreviveAUnaNuevaTransaccionEnLaMismaConexionFisica()
    {
        var tenantId = await fixture.CreateTenantAsync("nolek");
        await fixture.CreateClientAsync(tenantId, "Cliente No-Leak");

        await using var connection = await fixture.OpenAppConnectionAsync();

        // Transacción 1: fija el tenant, confirma que ve su fila, hace COMMIT.
        await using (var tx1 = await connection.BeginTransactionAsync())
        {
            await SetLocalTenantAsync(connection, tenantId);

            await using var command1 = connection.CreateCommand();
            command1.CommandText = "SELECT count(*) FROM public.clients;";
            var countWithTenant = (long)(await command1.ExecuteScalarAsync() ?? 0L);
            Assert.Equal(1, countWithTenant);

            await tx1.CommitAsync();
        }

        // Transacción 2: MISMA conexión física, SIN volver a fijar el tenant.
        // Si SET LOCAL "se filtrara" fuera de su transacción (comportamiento
        // incorrecto que este test existe para descartar), aquí seguiría
        // viendo la fila del tenant anterior.
        await using (var tx2 = await connection.BeginTransactionAsync())
        {
            await using var command2 = connection.CreateCommand();
            command2.CommandText = "SELECT count(*) FROM public.clients;";
            var countWithoutTenant = (long)(await command2.ExecuteScalarAsync() ?? 0L);

            Assert.Equal(0, countWithoutTenant);

            await tx2.CommitAsync();
        }
    }

    [Fact]
    public async Task Insert_ConTenantIdDistintoAlDeLaSesion_EsRechazadoPorWithCheck()
    {
        var tenantA = await fixture.CreateTenantAsync("check-a");
        var tenantB = await fixture.CreateTenantAsync("check-b");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetLocalTenantAsync(connection, tenantA);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO public.clients (id, tenant_id, legal_name, is_active)
            VALUES (@id, @tenantId, @legalName, true);
            """;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        // tenant_id de la fila NO coincide con el tenant de la sesión (tenantA)
        // — la policy WITH CHECK debe rechazar el INSERT.
        command.Parameters.AddWithValue("tenantId", tenantB);
        command.Parameters.AddWithValue("legalName", "Cliente Cross-Tenant Insert");

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Equal("42501", exception.SqlState);
    }

    [Fact]
    public async Task ProcofaApp_NuncaEsSuperusuarioNiTieneBypassRls()
    {
        await using var connection = await fixture.OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT rolsuper, rolbypassrls
              FROM pg_roles
             WHERE rolname = 'procofa_app';
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        Assert.False(reader.GetBoolean(0), "procofa_app no debe ser superusuario.");
        Assert.False(reader.GetBoolean(1), "procofa_app no debe tener BYPASSRLS.");
    }

    /// <summary>
    /// <c>SELECT set_config('app.tenant_id', tenantId, true)</c> — mismo
    /// mecanismo exacto que <c>TenantUnitOfWork.SetLocalTenantAsync</c> en
    /// Infrastructure, para que estos tests validen el comportamiento real
    /// que esa clase depende.
    /// </summary>
    private static async Task SetLocalTenantAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', @tenantId, true);";
        command.Parameters.AddWithValue("tenantId", tenantId.ToString());
        await command.ExecuteNonQueryAsync();
    }
}
