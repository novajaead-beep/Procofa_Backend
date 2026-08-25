using Npgsql;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests;

/// <summary>
/// Valida que <c>db/baseline/v2.1/{001_schema.sql,002_security.sql,003_seed_catalogs.sql}</c>
/// reconstruyen un esquema estructuralmente equivalente al baseline V2.1
/// real (Instrucción 03, sección "schema parity"). NO ejecutados por Claude
/// en este sandbox (Docker inalcanzable) — ver sección J/L del reporte.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class SchemaBootstrapTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task Bootstrap_Crea48TablasEnPublic()
    {
        await using var connection = await fixture.OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)
              FROM pg_tables
             WHERE schemaname = 'public';
            """;

        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);

        Assert.Equal(48, count);
    }

    [Fact]
    public async Task Bootstrap_Las36TablasEsperadasTienenForceRowLevelSecurity()
    {
        await using var connection = await fixture.OpenSuperuserConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT count(*)
              FROM pg_class c
              JOIN pg_namespace n ON n.oid = c.relnamespace
             WHERE n.nspname = 'public'
               AND c.relkind = 'r'
               AND c.relrowsecurity = true
               AND c.relforcerowsecurity = true;
            """;

        var count = (long)(await command.ExecuteScalarAsync() ?? 0L);

        Assert.Equal(36, count);
    }

    [Fact]
    public async Task Bootstrap_ProcofaApp_NoTienePrivilegioUpdateNiDeleteSobreAuditLogs()
    {
        // Ejercido contra pg_catalog directamente (no requiere SET LOCAL de
        // tenant) — el ACL es una propiedad del rol, no de la fila.
        await using var connection = await fixture.OpenAppConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                has_table_privilege('procofa_app', 'public.audit_logs', 'UPDATE') AS can_update,
                has_table_privilege('procofa_app', 'public.audit_logs', 'DELETE') AS can_delete,
                has_table_privilege('procofa_app', 'public.audit_logs', 'INSERT') AS can_insert,
                has_table_privilege('procofa_app', 'public.audit_logs', 'SELECT') AS can_select;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        Assert.False(reader.GetBoolean(reader.GetOrdinal("can_update")));
        Assert.False(reader.GetBoolean(reader.GetOrdinal("can_delete")));
        Assert.True(reader.GetBoolean(reader.GetOrdinal("can_insert")));
        Assert.True(reader.GetBoolean(reader.GetOrdinal("can_select")));
    }
}
