using System.Reflection;

namespace Procofa.Application.Tests;

/// <summary>
/// Procofa.Application no debe conocer Infrastructure ni Api (Instrucción 02,
/// sección 20) — solo depende de Domain.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly ApplicationAssembly = Assembly.Load("Procofa.Application");

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Procofa.Infrastructure",
        "Procofa.Api",
    ];

    [Fact]
    public void ElEnsamblado_CargaCorrectamente_YSeLlamaProcofaApplication()
    {
        Assert.Equal("Procofa.Application", ApplicationAssembly.GetName().Name);
    }

    [Fact]
    public void Application_NoReferenciaInfrastructureNiApi()
    {
        var referencedNames = ApplicationAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        var violaciones = referencedNames
            .Where(name => ForbiddenReferencePrefixes.Any(
                prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violaciones.Length == 0,
            $"Procofa.Application no debe referenciar: [{string.Join(", ", violaciones)}]. " +
                "Application define puertos y casos de uso; solo depende de Domain " +
                "(Instrucción 02, secciones 4 y 10).");
    }

    // Nota deliberada: no hay un test "Application debe referenciar Domain" a nivel
    // de ensamblado compilado. Con Application todavía vacía (sin casos de uso reales),
    // el compilador no emite un AssemblyRef a Procofa.Domain en el manifiesto aunque el
    // ProjectReference esté correctamente declarado en el .csproj — Assembly.GetReferencedAssemblies()
    // solo refleja tipos efectivamente usados, no el grafo de build. Esa dirección de la regla
    // ya la garantiza `dotnet build` (falla si el ProjectReference no resuelve); un test de
    // runtime aquí sería redundante y, mientras Domain siga vacío, daría un falso negativo.
}
