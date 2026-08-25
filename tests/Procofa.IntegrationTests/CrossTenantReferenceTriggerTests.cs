using Npgsql;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests;

/// <summary>
/// Prueba <c>enforce_same_tenant_references()</c> (Instrucción 03 — una de
/// las 6 funciones PL/pgSQL del baseline V2.1, sección D del reporte):
/// segunda capa de integridad referencial multitenant, MÁS ALLÁ de RLS —
/// bloquea a nivel de trigger que una fila de un tenant referencie (vía FK)
/// una fila de OTRO tenant, incluso si ambas filas fueran visibles en la
/// misma sesión (lo que no debería ocurrir de todas formas, por RLS —
/// esta es una capa de defensa adicional, no redundante: cubre el caso de
/// un bug de aplicación que arme mal el <c>tenant_id</c> de la fila nueva).
///
/// Usa <c>client_contacts</c> (FK a <c>clients</c>) por ser el ejemplo con
/// menos dependencias de todo el grafo — mismo patrón aplica a las
/// ~20 tablas restantes que llevan este trigger. Ejecutado siempre como
/// <c>procofa_app</c>. NO ejecutado por Claude en este sandbox (Docker
/// inalcanzable) — ver sección J/L del reporte de Instrucción 03.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class CrossTenantReferenceTriggerTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task Insert_ConReferenciaAFilaDeOtroTenant_LanzaExcepcion()
    {
        var tenantA = await fixture.CreateTenantAsync("xref-a");
        var tenantB = await fixture.CreateTenantAsync("xref-b");
        var clientA = await fixture.CreateClientAsync(tenantA, "Cliente Tenant A");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetLocalTenantAsync(connection, tenantB);

        // La fila es de tenantB, pero client_id apunta a un cliente de
        // tenantA -- enforce_same_tenant_references debe rechazarlo.
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO public.client_contacts (id, tenant_id, client_id, first_name, last_name, is_active)
            VALUES (@id, @tenantId, @clientId, 'Nombre', 'Apellido', true);
            """;
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("tenantId", tenantB);
        command.Parameters.AddWithValue("clientId", clientA);

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => command.ExecuteNonQueryAsync());

        Assert.Contains(
            "Referencia inválida/no visible",
            exception.MessageText,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Insert_ConReferenciaAFilaDelMismoTenant_TieneExito()
    {
        var tenantA = await fixture.CreateTenantAsync("xref-ok");
        var clientA = await fixture.CreateClientAsync(tenantA, "Cliente Mismo Tenant");

        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetLocalTenantAsync(connection, tenantA);

        var contactId = Guid.NewGuid();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO public.client_contacts (id, tenant_id, client_id, first_name, last_name, is_active)
            VALUES (@id, @tenantId, @clientId, 'Nombre', 'Apellido', true);
            """;
        command.Parameters.AddWithValue("id", contactId);
        command.Parameters.AddWithValue("tenantId", tenantA);
        command.Parameters.AddWithValue("clientId", clientA);

        var affected = await command.ExecuteNonQueryAsync();

        Assert.Equal(1, affected);
    }

    private static async Task SetLocalTenantAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', @tenantId, true);";
        command.Parameters.AddWithValue("tenantId", tenantId.ToString());
        await command.ExecuteNonQueryAsync();
    }
}
