using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Application.Abstractions.Checklists;
using Procofa.Application.Abstractions.Clients;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Infrastructure.Persistence;
using Procofa.Infrastructure.Persistence.Interceptors;
using Procofa.Infrastructure.Persistence.Repositories;
using Procofa.Infrastructure.Persistence.Tenancy;
using Procofa.Infrastructure.Security;

namespace Procofa.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// CERRAR AUTH: la connection string de <c>ProcofaDb</c> ya NO se recibe como <c>string?</c> ya
    /// resuelto por el caller. Se resuelve de forma DIFERIDA, dentro del delegate <c>(sp, options)
    /// =&gt;</c> de <see
    /// cref="M:Microsoft.EntityFrameworkCore.EntityFrameworkServiceCollectionExtensions.AddDbContext``1(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{System.IServiceProvider,Microsoft.EntityFrameworkCore.DbContextOptionsBuilder},Microsoft.Extensions.DependencyInjection.ServiceLifetime,Microsoft.Extensions.DependencyInjection.ServiceLifetime)"/>,
    /// leyendo <see cref="IConfiguration"/> desde el <see cref="IServiceProvider"/> recién en el
    /// momento en que <see cref="ProcofaDbContext"/> se construye por primera vez (primera
    /// resolución real, por scope/request — después de que el host terminó <c>Build()</c>). Mismo
    /// patrón ya aplicado a <c>InfrastructureAuthSettings</c> — necesario porque
    /// <c>WebApplicationFactory&lt;Program&gt;.ConfigureAppConfiguration</c> añade su override de
    /// <c>ConnectionStrings:ProcofaDb</c> DESPUÉS de que <c>Program.cs</c> corre pero ANTES de la
    /// primera resolución real de <see cref="ProcofaDbContext"/> — leerla antes de <c>Build()</c>
    /// (como hacía la versión anterior) capturaba el valor de <c>appsettings</c> local, nunca el
    /// del fixture de test. </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<ProcofaDbContext>((sp, options) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("ProcofaDb");

            // Nunca se lanza por connectionString nulo/vacío — ver justificación
            // histórica en el comentario de clase. El placeholder es únicamente
            // para que un eventual error de conexión sea autoexplicativo.
            var effectiveConnectionString = string.IsNullOrWhiteSpace(connectionString)
                ? "Host=localhost;Database=procofa_connectionstring_not_configured;Username=none;Password=none"
                : connectionString;

            options.UseNpgsql(effectiveConnectionString)
                .AddInterceptors(new ConcurrencyTokenInterceptor());
        });

        services.AddAuth();
        return services;
    }

    private static void AddAuth(this IServiceCollection services)
    {
        // ITenantContext/ITenantUnitOfWork se registran aquí — antes quedaban deliberadamente sin
        // registrar hasta que existiera un consumidor real de autenticación.
        services.AddScoped<ITenantContext>(sp =>
          {
              var settings = sp.GetRequiredService<InfrastructureAuthSettings>();
              return new Stage1TenantContext(settings.ProcofaTenantId);
          });
        services.AddScoped<ITenantUnitOfWork, TenantUnitOfWork>();

        // Repositorios de Auth (específicos, no genéricos — "No GenericRepository").
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAccessLogRepository, AccessLogRepository>();

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IAuditedCompanyRepository, AuditedCompanyRepository>();
        services.AddScoped<ICompanySiteRepository, CompanySiteRepository>();
        services.AddScoped<IClientContactRepository, ClientContactRepository>();
        services.AddScoped<IProgramRepository, ProgramRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IAuditTypeRepository, AuditTypeRepository>();

        services.AddScoped<IChecklistRepository, ChecklistRepository>();
        services.AddScoped<IChecklistVersionRepository, ChecklistVersionRepository>();
        services.AddScoped<IChecklistSectionRepository, ChecklistSectionRepository>();
        services.AddScoped<ICriterionRepository, CriterionRepository>();

        // Adapters de seguridad — sin estado propio salvo la config ya resuelta, seguros como singleton.
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<IRefreshTokenFactory, RefreshTokenFactory>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddSingleton<IAccessTokenGenerator>(sp =>
{
    var settings = sp.GetRequiredService<InfrastructureAuthSettings>();

    return new JwtAccessTokenGenerator(
        settings.JwtIssuer,
        settings.JwtAudience,
        settings.JwtSigningKey,
        settings.JwtAccessTokenMinutes);
});

       services.AddSingleton<IAuthPolicyOptions>(sp =>
{
    var settings = sp.GetRequiredService<InfrastructureAuthSettings>();
    return new AuthPolicyOptionsAdapter(settings);
});
    }
}
