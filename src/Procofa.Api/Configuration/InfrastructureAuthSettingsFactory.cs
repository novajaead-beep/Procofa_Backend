using Procofa.Infrastructure;

namespace Procofa.Api.Configuration;

/// <summary>
/// Traduce <see cref="IConfiguration"/> (appsettings/environment/user-secrets)
/// a <see cref="InfrastructureAuthSettings"/> (Instrucción 04). Único punto
/// del proceso que conoce las claves de configuración de Auth — compartido
/// por el arranque web normal (<c>Program.cs</c>) y por el host mode
/// <c>bootstrap-admin</c> (<see cref="Procofa.Api.Bootstrap.BootstrapAdminRunner"/>),
/// para no duplicar la forma de la configuración en dos lugares.
///
/// Nota: NO valida aquí que <c>Jwt:SigningKey</c> tenga longitud suficiente
/// ni que los valores numéricos sean positivos — esa validación de dominio
/// vive en los constructores de <c>JwtAccessTokenGenerator</c> y
/// <c>AuthPolicyOptionsAdapter</c> (Infrastructure), que son quienes conocen
/// las reglas reales (256 bits mínimo para HS256, etc.). Esta clase solo
/// resuelve strings/ints crudos desde configuración con sus defaults.
/// </summary>
internal static class InfrastructureAuthSettingsFactory
{
    private const string DefaultProcofaTenantId = "00000000-0000-0000-0000-000000000001";

    public static InfrastructureAuthSettings Create(IConfiguration configuration)
    {
        var tenantIdRaw = configuration["Tenancy:ProcofaTenantId"] ?? DefaultProcofaTenantId;
        if (!Guid.TryParse(tenantIdRaw, out var tenantId))
        {
            throw new InvalidOperationException(
                $"Tenancy:ProcofaTenantId ('{tenantIdRaw}') no es un GUID válido.");
        }

        return new InfrastructureAuthSettings(
            ProcofaTenantId: tenantId,
            JwtIssuer: configuration["Jwt:Issuer"] ?? string.Empty,
            JwtAudience: configuration["Jwt:Audience"] ?? string.Empty,
            JwtSigningKey: configuration["Jwt:SigningKey"] ?? string.Empty,
            JwtAccessTokenMinutes: configuration.GetValue("Jwt:AccessTokenMinutes", 15),
            AuthMaxFailedLoginAttempts: configuration.GetValue("Auth:MaxFailedLoginAttempts", 5),
            AuthLockoutMinutes: configuration.GetValue("Auth:LockoutMinutes", 15),
            AuthRefreshTokenDays: configuration.GetValue("Auth:RefreshTokenDays", 30));
    }
}
