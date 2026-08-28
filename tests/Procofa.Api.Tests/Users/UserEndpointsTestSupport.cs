using Procofa.Infrastructure.Security;
using Procofa.IntegrationTests.Auth;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.Api.Tests.Users;

/// <summary> emite JWTs de prueba directamente (sin pasar por <c>POST /api/auth/login</c>) para
/// ejercer 401/403/200 sin depender de que exista un usuario real con esas credenciales. Los
/// valores de Issuer/Audience/SigningKey deben coincidir EXACTAMENTE con los que <see
/// cref="UserEndpointsTests"/> inyecta vía <c>WebApplicationFactory</c> (mismo criterio que
/// <c>LoginEndpointTests</c>: "las credenciales del fixture y las usadas por la API deben ser
/// exactamente las mismas"). </summary>
internal static class UserEndpointsTestSupport
{
    public const string JwtIssuer = "procofa-api-tests";
    public const string JwtAudience = "procofa-api-tests";
    public const string JwtSigningKey = "clave-de-firma-de-pruebas-api-de-al-menos-32-bytes";

    public static string CreateToken(Guid userId, params string[] roleCodes)
    {
        var generator = new JwtAccessTokenGenerator(JwtIssuer, JwtAudience, JwtSigningKey, accessTokenMinutes: 15);
        return generator
            .GenerateAccessToken(userId, AuthHandlerFactory.ProcofaTenantId, "test-token@procofa.com", roleCodes)
            .Value;
    }

    /// <summary>
    /// los endpoints de escritura (ej. <c>POST /api/users</c>) usan <c>ICurrentUser.UserId</c> (el
    /// <c>sub</c> del JWT) como <c>user_roles.assigned_by_user_id</c> — el trigger
    /// <c>enforce_same_tenant_references()</c> exige que ese id exista físicamente en <c>users</c>
    /// dentro del mismo tenant. <see cref="CreateToken"/> por sí solo NUNCA persiste un usuario
    /// real, así que cualquier test que autentique como ADMIN y ejecute una escritura debe sembrar
    /// primero un usuario+rol ADMIN real con EXACTAMENTE el mismo id que el <c>sub</c> del token —
    /// vía <see cref="PostgresBaselineFixture.SuperuserConnectionString"/> (bootstrap de datos de
    /// prueba, nunca para las aserciones de RLS/ACL en sí, mismo criterio que el resto del
    /// fixture). </summary>
    public static async Task SeedAdminAsync(
        PostgresBaselineFixture fixture, Guid tenantId, Guid adminUserId, string email)
    {
        var adminRoleId = await fixture.GetCatalogIdByCodeAsync("roles", "ADMIN");

        await using var connection = await fixture.OpenSuperuserConnectionAsync();

        await using (var userCommand = connection.CreateCommand())
        {
            userCommand.CommandText = """
                INSERT INTO public.users (id, tenant_id, email, password_hash, first_name, last_name, is_active)
                VALUES (@id, @tenantId, @email, @passwordHash, 'Test', 'Admin', true);
                """;
            userCommand.Parameters.AddWithValue("id", adminUserId);
            userCommand.Parameters.AddWithValue("tenantId", tenantId);
            userCommand.Parameters.AddWithValue("email", email);
            userCommand.Parameters.AddWithValue("passwordHash", "test-only-not-a-real-hash");
            await userCommand.ExecuteNonQueryAsync();
        }

        await using (var roleCommand = connection.CreateCommand())
        {
            roleCommand.CommandText = """
                INSERT INTO public.user_roles (tenant_id, user_id, role_id)
                VALUES (@tenantId, @userId, @roleId);
                """;
            roleCommand.Parameters.AddWithValue("tenantId", tenantId);
            roleCommand.Parameters.AddWithValue("userId", adminUserId);
            roleCommand.Parameters.AddWithValue("roleId", adminRoleId);
            await roleCommand.ExecuteNonQueryAsync();
        }
    }
}
