using Microsoft.EntityFrameworkCore;
using Procofa.Adapters.FileStorage;
using Procofa.Adapters.Persistence.PostgreSQL;
using Procofa.Adapters.Persistence.PostgreSQL.Repositories;
using Procofa.Adapters.ReportGeneration;
using Procofa.Application.Ports.Out;

var builder = WebApplication.CreateBuilder(args);

// --- Composition root: wiring de adaptadores contra los puertos de salida (hexagonal) ---
builder.Services.AddDbContext<ProcofaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProcofaDb")));

builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IReportGeneratorPort, OpenXmlReportGenerator>();
builder.Services.AddScoped<IEvidenceStoragePort, ObjectStorageEvidenceAdapter>();

builder.Services.AddControllers();
// TODO: registrar el ensamblado de Procofa.Adapters.Api.Rest como Application Part
//       (builder.Services.AddControllers().AddApplicationPart(typeof(SomeController).Assembly)).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
