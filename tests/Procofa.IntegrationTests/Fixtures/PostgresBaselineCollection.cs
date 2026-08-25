namespace Procofa.IntegrationTests.Fixtures;

/// <summary>
/// Une todas las clases de test bajo un ÚNICO <see cref="PostgresBaselineFixture"/>
/// — un solo contenedor PostgreSQL 18 para toda la suite, en vez de uno por
/// clase (levantar Testcontainers es costoso; xUnit garantiza que las clases
/// de una misma collection NO corren en paralelo entre sí, evitando
/// condiciones de carrera sobre el mismo contenedor).
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresBaselineCollection : ICollectionFixture<PostgresBaselineFixture>
{
    public const string Name = "PostgresBaseline";
}
