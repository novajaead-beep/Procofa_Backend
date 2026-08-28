using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Procofa.Api.Bootstrap;
using Procofa.Api.Configuration;
using Procofa.Api.Endpoints;
using Procofa.Api.Security;
using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.UseCases.Auth.Login;
using Procofa.Application.UseCases.Users.ChangeUserStatus;
using Procofa.Application.UseCases.Users.CreateUser;
using Procofa.Application.UseCases.Users.GetUser;
using Procofa.Application.UseCases.Users.ListUsers;
using Procofa.Application.UseCases.Users.ReplaceUserClientAccess;
using Procofa.Application.UseCases.Users.ReplaceUserRoles;
using Procofa.Infrastructure;

// Host mode explícito, NUNCA un endpoint HTTP. Debe interceptarse ANTES de
// WebApplication.CreateBuilder para no levantar Kestrel/el pipeline HTTP completo por un comando de
// un solo disparo — ver BootstrapAdminRunner para el comando exacto.
if (args.Length > 0 && string.Equals(args[0], "bootstrap-admin", StringComparison.OrdinalIgnoreCase))
{
    return await BootstrapAdminRunner.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

// CERRAR AUTH: ya NO se lee "ConnectionStrings:ProcofaDb" aquí. AddInfrastructure() registra
// ProcofaDbContext con el delegate (sp, options) => ... de AddDbContext, que resuelve
// IConfiguration desde el IServiceProvider recién en la primera resolución real del DbContext (por
// scope/request) — no en este punto, antes de builder.Build(). Ver
// Procofa.Infrastructure/DependencyInjection.cs para el detalle completo. Ausencia de
// "ConnectionStrings:ProcofaDb" NO impide arrancar: AddDbContext sigue siendo perezoso y "/health"
// no toca ProcofaDbContext.
builder.Services.AddSingleton(sp =>
    InfrastructureAuthSettingsFactory.Create(
        sp.GetRequiredService<IConfiguration>()));

builder.Services.AddInfrastructure();

// Sin AddApplication(IServiceCollection) propio todavía (Application sigue sin depender de
// Microsoft.Extensions.DependencyInjection.Abstractions), se registra aquí directamente en el
// Composition Root.
builder.Services.AddScoped<LoginCommandHandler>();

// Casos de uso de gestión de usuarios — mismo Composition Root, mismo criterio (registro directo,
// sin contenedor DI en Application).
builder.Services.AddScoped<ListUsersQueryHandler>();
builder.Services.AddScoped<GetUserQueryHandler>();
builder.Services.AddScoped<CreateUserCommandHandler>();
builder.Services.AddScoped<ChangeUserStatusCommandHandler>();
builder.Services.AddScoped<ReplaceUserRolesCommandHandler>();
builder.Services.AddScoped<ReplaceUserClientAccessCommandHandler>();

// ICurrentUser: implementación HTTP vive en Api (Procofa.Api.Security.HttpContextCurrentUser) —
// Application solo conoce el puerto. Scoped porque lee HttpContext.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// Primera vez que el proyecto exige JWT — hasta ahora solo se EMITÍA el token (login), nunca se
// VALIDABA en un endpoint protegido. Igual que ProcofaDb/InfrastructureAuthSettings, la validación
// se registra vía AddOptions<T>().Configure<TDep>(...) — el delegate recibe
// InfrastructureAuthSettings resuelto desde DI de forma diferida (primera resolución real de
// JwtBearerOptions, no en este punto), para que WebApplicationFactory pueda seguir sobreescribiendo
// Jwt:SigningKey en tests exactamente como ya hace con ProcofaDb.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<InfrastructureAuthSettings>((options, settings) =>
    {
        // MapInboundClaims=false (default en .NET 8+, explícito aquí a
        // propósito): los claims del token quedan EXACTAMENTE como los emite
        // JwtAccessTokenGenerator ("sub", "roles", "tenant_id", ...) — sin la
        // conversión legacy a URIs de System.Security.Claims.ClaimTypes.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = settings.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            // JwtAccessTokenGenerator emite un claim "roles" por rol (no el
            // URI legacy ClaimTypes.Role) — sin esto, [Authorize(Roles=...)]
            // / RequireRole(...) nunca reconocerían los roles del token.
            RoleClaimType = "roles",
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// "Config validation al startup": los constructores de JwtAccessTokenGenerator
// y AuthPolicyOptionsAdapter validan Jwt:SigningKey/Issuer/Audience y los
// valores numéricos de Auth — pero como están registrados como singleton vía
// factory delegate, EF/DI no los invoca hasta la primera resolución real.
// Se fuerza esa resolución aquí, antes de aceptar tráfico, para fallar en
// el arranque (y no en el primer POST /api/auth/login de un usuario real).
app.Services.GetRequiredService<IAccessTokenGenerator>();
app.Services.GetRequiredService<IAuthPolicyOptions>();

// "ProblemDetails sin stack trace en respuesta": cualquier excepción no
// manejada por debajo de este punto responde con un ProblemDetails genérico
// — el detalle real solo va al log del servidor, nunca al cliente.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature is not null)
        {
            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("Procofa.Api.UnhandledException");
            logger.LogError(exceptionFeature.Error, "Excepción no manejada procesando {Path}", context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Ha ocurrido un error interno.",
            Detail = "Ocurrió un error inesperado procesando la solicitud. Intente nuevamente más tarde.",
        };

        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponseAsync
});

app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();
return 0;

static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync($$"""{"status":"{{report.Status}}"}""");
}

// Expone el tipo de entry point para WebApplicationFactory<Program> en
// Procofa.Api.Tests — los top-level statements generan "Program" internal por defecto; esta
// declaración lo hace visible desde el assembly de tests sin exponer nada más.
public partial class Program;
