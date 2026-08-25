using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Procofa.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

// AddInfrastructure recibe la connection string ya resuelta como string (no
// IConfiguration) — ver justificación en Procofa.Infrastructure/
// DependencyInjection.cs. Ausencia de "ConnectionStrings:ProcofaDb" NO
// impide arrancar: AddDbContext es perezoso y "/health" no toca
// ProcofaDbContext (Instrucción 03: "/health debe funcionar sin conectarse
// a la BD").
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("ProcofaDb"));

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponseAsync
});

app.Run();

static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync($$"""{"status":"{{report.Status}}"}""");
}
