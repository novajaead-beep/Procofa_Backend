using System.Reflection;

namespace Procofa.IntegrationTests;

/// <summary>
/// Foundation (Instrucción 02): solo verifica que Procofa.IntegrationTests está
/// correctamente conectado a Infrastructure/Application/Domain. Las pruebas reales
/// de aislamiento RLS contra PostgreSQL (Testcontainers, imagen postgres:18) llegan
/// en la Instrucción 03 — no se introduce esa infraestructura todavía (sección 5).
/// </summary>
public class ProjectWiringTests
{
    [Fact]
    public void InfrastructureAssembly_SeCargaCorrectamente()
    {
        var assembly = Assembly.Load("Procofa.Infrastructure");
        Assert.Equal("Procofa.Infrastructure", assembly.GetName().Name);
    }

    [Fact]
    public void ApplicationAssembly_SeCargaCorrectamente()
    {
        var assembly = Assembly.Load("Procofa.Application");
        Assert.Equal("Procofa.Application", assembly.GetName().Name);
    }

    [Fact]
    public void DomainAssembly_SeCargaCorrectamente()
    {
        var assembly = Assembly.Load("Procofa.Domain");
        Assert.Equal("Procofa.Domain", assembly.GetName().Name);
    }
}
