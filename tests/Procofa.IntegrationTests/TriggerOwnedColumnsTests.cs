using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests;

/// <summary>
/// Prueba las dos funciones PL/pgSQL "propietarias de columna" del baseline
/// V2.1 (Instrucción 03, sección D): <c>set_updated_at_utc()</c> (36 tablas
/// la usan — <c>updated_at_utc</c> NUNCA la escribe EF, ver
/// <see cref="Procofa.Infrastructure.Persistence.ProcofaDbContext"/> y el
/// comentario <c>ValueGeneratedOnAddOrUpdate()</c> en cada configuración) y
/// <c>normalize_user_email()</c> (solo <c>users</c>).
///
/// Ambos tests corren como SUPERUSUARIO por simplicidad — ninguno ejercita
/// RLS/ACL (ya cubierto exhaustivamente en <c>RlsTenantIsolationTests</c> y
/// <c>SchemaBootstrapTests</c>); aquí el único comportamiento bajo prueba es
/// el trigger en sí sobre una fila ya conocida.
///
/// NO ejecutados por Claude en este sandbox (Docker inalcanzable) — ver
/// sección J/L del reporte de Instrucción 03.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class TriggerOwnedColumnsTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task Users_UpdateSinTocarEmail_ActualizaUpdatedAtUtc_ViaTrigger()
    {
        var tenantId = await fixture.CreateTenantAsync("upd-at");
        var userId = await fixture.CreateUserAsync(tenantId, "updated-at");

        await using var connection = await fixture.OpenSuperuserConnectionAsync();

        DateTime updatedAtBefore;
        await using (var selectBefore = connection.CreateCommand())
        {
            selectBefore.CommandText = "SELECT updated_at_utc FROM public.users WHERE id = @id;";
            selectBefore.Parameters.AddWithValue("id", userId);
            updatedAtBefore = (DateTime)(await selectBefore.ExecuteScalarAsync())!;
        }

        // Pausa breve para que NOW() durante el UPDATE quede estrictamente
        // después del timestamp de creación, aun en relojes de resolución
        // gruesa -- evita un falso positivo si ambos timestamps cayeran en
        // el mismo instante medible.
        await Task.Delay(TimeSpan.FromMilliseconds(50));

        await using (var updateCommand = connection.CreateCommand())
        {
            // Se actualiza first_name, NUNCA email -- trg_users_normalize_email
            // es "BEFORE INSERT OR UPDATE OF email" y no debe interferir; este
            // test aísla exclusivamente trg_users_updated_at.
            updateCommand.CommandText = "UPDATE public.users SET first_name = 'ActualizadoPorTest' WHERE id = @id;";
            updateCommand.Parameters.AddWithValue("id", userId);
            await updateCommand.ExecuteNonQueryAsync();
        }

        DateTime updatedAtAfter;
        await using (var selectAfter = connection.CreateCommand())
        {
            selectAfter.CommandText = "SELECT updated_at_utc FROM public.users WHERE id = @id;";
            selectAfter.Parameters.AddWithValue("id", userId);
            updatedAtAfter = (DateTime)(await selectAfter.ExecuteScalarAsync())!;
        }

        Assert.True(
            updatedAtAfter > updatedAtBefore,
            $"updated_at_utc debía avanzar tras el UPDATE (antes={updatedAtBefore:o}, después={updatedAtAfter:o}).");
    }

    [Fact]
    public async Task Users_UpdateEmail_NormalizaEmailYNormalizedEmail_ViaTrigger()
    {
        var tenantId = await fixture.CreateTenantAsync("norm-email");
        var userId = await fixture.CreateUserAsync(tenantId, "normalizacion");

        // Email deliberadamente con espacios circundantes y mayúsculas
        // mezcladas -- ejercita BTRIM (en email) + UPPER(BTRIM(...)) (en
        // normalized_email) exactamente como los define normalize_user_email().
        const string rawEmail = "  Nuevo.Correo@Ejemplo.COM  ";

        await using var connection = await fixture.OpenSuperuserConnectionAsync();

        await using (var updateCommand = connection.CreateCommand())
        {
            updateCommand.CommandText = "UPDATE public.users SET email = @email WHERE id = @id;";
            updateCommand.Parameters.AddWithValue("email", rawEmail);
            updateCommand.Parameters.AddWithValue("id", userId);
            await updateCommand.ExecuteNonQueryAsync();
        }

        await using var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT email, normalized_email FROM public.users WHERE id = @id;";
        selectCommand.Parameters.AddWithValue("id", userId);
        await using var reader = await selectCommand.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());

        // email conserva mayúsculas/minúsculas originales, solo recortado.
        Assert.Equal("Nuevo.Correo@Ejemplo.COM", reader.GetString(0));
        // normalized_email es el recortado en mayúsculas -- la columna que
        // sostiene la unicidad case-insensitive real por tenant (constraint
        // uq_users_tenant_normalized_email UNIQUE (tenant_id, normalized_email)
        // en el baseline).
        Assert.Equal("NUEVO.CORREO@EJEMPLO.COM", reader.GetString(1));
    }
}
