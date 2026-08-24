using System.Reflection;

namespace Procofa.Domain.Tests;

/// <summary>
/// Verifica, por reflection sobre el ensamblado ya compilado, que Procofa.Domain
/// se mantiene libre de dependencias de infraestructura (Instrucción 02, sección 20).
/// Ninguno de los dos tests es un Assert.True(true): cada uno falla de verdad si
/// la regla arquitectónica se rompe.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = Assembly.Load("Procofa.Domain");

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Procofa.Infrastructure",
        "Procofa.Api",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Microsoft.AspNetCore",
    ];

    [Fact]
    public void ElEnsamblado_CargaCorrectamente_YSeLlamaProcofaDomain()
    {
        Assert.Equal("Procofa.Domain", DomainAssembly.GetName().Name);
    }

    [Fact]
    public void Domain_NoReferenciaInfrastructureNiApi_NiEfCoreNiNpgsqlNiAspNetCore()
    {
        var referencedNames = DomainAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        var violaciones = referencedNames
            .Where(name => ForbiddenReferencePrefixes.Any(
                prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violaciones.Length == 0,
            $"Procofa.Domain no debe referenciar: [{string.Join(", ", violaciones)}]. " +
                "Domain debe permanecer libre de Infrastructure, Api, EF Core, Npgsql y ASP.NET Core " +
                "(Instrucción 02, secciones 4 y 9).");
    }
}
