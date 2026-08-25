using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Procofa.Infrastructure.Persistence;
using Procofa.Infrastructure.Persistence.Interceptors;

namespace Procofa.Infrastructure;

/// <summary>
/// Composition Root de Infrastructure (Instrucción 03, sección 42-43).
///
/// Registra ÚNICAMENTE <see cref="ProcofaDbContext"/> (+ el interceptor de
/// concurrencia) por ahora. <see cref="Persistence.Tenancy.TenantUnitOfWork"/>
/// existe, compila y es exercised directamente por integration tests (que
/// construyen su propio <c>ITenantContext</c> de prueba), pero
/// DELIBERADAMENTE NO se registra aquí todavía: su constructor requiere
/// <c>Procofa.Application.Abstractions.Tenancy.ITenantContext</c>, que no
/// tiene ninguna implementación real hasta la instrucción de Auth/JWT (fuera
/// de alcance de Instrucción 03). Registrarlo ahora rompería
/// <c>builder.Build()</c> en Development (<c>ValidateOnBuild=true</c> valida
/// el grafo de DI completo de forma eager y no encontraría quién implementa
/// <c>ITenantContext</c>) — justo lo que "/health debe seguir funcionando
/// sin conectarse a la BD" prohíbe romper. Se añadirá a este método en la
/// instrucción de Auth, junto con el registro real de <c>ITenantContext</c>.
///
/// Recibe la connection string ya resuelta como <c>string</c> — no
/// <c>IConfiguration</c> — para que Infrastructure no dependa de
/// <c>Microsoft.Extensions.Configuration.Abstractions</c> solo para esto;
/// <c>Procofa.Api/Program.cs</c> (el Composition Root real de la app) es
/// quien conoce la forma de la configuración
/// (<c>"ConnectionStrings:ProcofaDb"</c>).
///
/// <c>AddDbContext</c> no abre conexión alguna al registrar el servicio — el
/// delegate de configuración (y por lo tanto Npgsql) solo se evalúa la
/// PRIMERA vez que algo resuelve <see cref="ProcofaDbContext"/> desde el
/// contenedor. Nada en
/// esta instrucción lo resuelve (sin Controllers, sin health check basado en
/// BD), así que una connection string ausente/placeholder NUNCA rompe el
/// arranque de la Api ni <c>/health</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? connectionString)
    {
        // Nunca se lanza por connectionString nulo/vacío — ver justificación
        // arriba. El placeholder es únicamente para que un eventual error de
        // conexión (cuando SÍ se use ProcofaDbContext, en una instrucción
        // futura) sea autoexplicativo en vez de una cadena vacía muda.
        var effectiveConnectionString = string.IsNullOrWhiteSpace(connectionString)
            ? "Host=localhost;Database=procofa_connectionstring_not_configured;Username=none;Password=none"
            : connectionString;

        services.AddDbContext<ProcofaDbContext>(options =>
            options.UseNpgsql(effectiveConnectionString)
                .AddInterceptors(new ConcurrencyTokenInterceptor()));

        return services;
    }
}
