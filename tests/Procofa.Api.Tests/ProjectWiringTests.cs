using System.Reflection;

namespace Procofa.Api.Tests;

/// <summary>
/// Foundation (Instrucción 02): solo verifica que Procofa.Api.Tests está
/// correctamente conectado a Procofa.Api. Las pruebas HTTP end-to-end con
/// WebApplicationFactory (Microsoft.AspNetCore.Mvc.Testing) llegan cuando exista
/// un endpoint funcional real que valga la pena probar así (sección 5: "Posteriormente
/// Api.Tests podrá utilizar WebApplicationFactory").
/// </summary>
public class ProjectWiringTests
{
    [Fact]
    public void ApiAssembly_SeCargaCorrectamente()
    {
        var assembly = Assembly.Load("Procofa.Api");
        Assert.Equal("Procofa.Api", assembly.GetName().Name);
    }
}
