using Procofa.IntegrationTests.Fixtures;

namespace Procofa.Api.Tests;

/// <summary>
/// xUnit no comparte <c>[CollectionDefinition]</c> entre assemblies de test
/// — <c>PostgresBaselineCollection</c> vive en <c>Procofa.IntegrationTests</c>
/// y solo aplica dentro de ESE assembly. Esta clase declara la misma
/// definición (mismo <see cref="PostgresBaselineFixture"/>, mismo nombre de
/// colección para legibilidad) dentro de <c>Procofa.Api.Tests</c>, así que
/// correr ambos proyectos de test levanta dos contenedores Postgres
/// independientes — esperado y aceptable (evita acoplar el ciclo de vida de
/// un assembly de test al de otro).
/// </summary>
[CollectionDefinition(PostgresBaselineCollection.Name)]
public sealed class ApiPostgresBaselineCollection : ICollectionFixture<PostgresBaselineFixture>
{
}
