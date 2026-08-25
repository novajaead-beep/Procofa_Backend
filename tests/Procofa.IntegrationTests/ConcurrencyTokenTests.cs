using Microsoft.EntityFrameworkCore;
using Procofa.IntegrationTests.Fixtures;

namespace Procofa.IntegrationTests;

/// <summary>
/// Prueba <c>ConcurrencyTokenInterceptor</c> (Infrastructure) + el chequeo
/// nativo de concurrencia optimista de EF Core sobre <c>lock_version</c>
/// (Instrucción 03, sección H): dos
/// <see cref="Procofa.Infrastructure.Persistence.ProcofaDbContext"/>
/// independientes cargan la MISMA fila de <c>audit_criteria</c>; el primero
/// guarda con éxito (el interceptor incrementa <c>lock_version</c> en
/// memoria antes del UPDATE físico); el segundo, con un <c>lock_version</c>
/// "viejo" en su snapshot original, debe fallar con
/// <see cref="DbUpdateConcurrencyException"/> — el WHERE que genera EF
/// incluye <c>lock_version = &lt;valor original del segundo contexto&gt;</c>,
/// que ya no coincide con la fila física tras el primer <c>SaveChanges</c>.
///
/// El valor se muta vía <c>context.Entry(entity).Property(...).CurrentValue</c>
/// en lugar de un setter público de <c>AuditCriterion</c> — a propósito: la
/// entidad de dominio deliberadamente NO expone mutadores todavía
/// (Instrucción 03 excluye CRUD/casos de uso de Application de esta etapa),
/// así que este test ejercita el mecanismo de persistencia de EF
/// directamente, sin adelantar comportamiento de dominio fuera de alcance.
///
/// Ambos contextos se conectan como SUPERUSUARIO (bypass real de RLS, no un
/// atajo de ACL) en vez de <c>procofa_app</c> + tenant <c>SET LOCAL</c>: el
/// aislamiento por tenant vía RLS ya está probado exhaustivamente en
/// <c>RlsTenantIsolationTests</c>; lo único que este test necesita es una
/// fila visible y estable para ejercitar el token de concurrencia en sí,
/// sin acoplar dos preocupaciones distintas en un solo test.
///
/// NO ejecutado por Claude en este sandbox (Docker inalcanzable) — ver
/// sección J/L del reporte de Instrucción 03.
/// </summary>
[Collection(PostgresBaselineCollection.Name)]
public sealed class ConcurrencyTokenTests(PostgresBaselineFixture fixture)
{
    [Fact]
    public async Task Update_ConLockVersionDesactualizado_LanzaDbUpdateConcurrencyException()
    {
        var tenantId = await fixture.CreateTenantAsync("concurrency");
        var userId = await fixture.CreateUserAsync(tenantId, "concurrency");
        var auditData = await fixture.CreateMinimalAuditAsync(tenantId, userId, "concurrency");
        var criterionId = await fixture.CreateAuditCriterionAsync(
            tenantId,
            auditData.AuditId,
            userId,
            isMandatorySnapshot: true,
            complianceStatusCode: null,
            suffix: "concurrency");

        await using var contextA = fixture.CreateDbContext(fixture.SuperuserConnectionString);
        await using var contextB = fixture.CreateDbContext(fixture.SuperuserConnectionString);

        var criterionInA = await contextA.AuditCriteria.SingleAsync(c => c.Id == criterionId);
        var criterionInB = await contextB.AuditCriteria.SingleAsync(c => c.Id == criterionId);

        // contextA guarda primero -- ConcurrencyTokenInterceptor incrementa su
        // lock_version en memoria (1 -> 2) antes del UPDATE físico, que tiene
        // éxito (el WHERE de este UPDATE todavía usa lock_version = 1, que sí
        // coincide con la fila física en este momento).
        contextA.Entry(criterionInA).Property(nameof(criterionInA.AuditedResponse)).CurrentValue = "Respuesta de A";
        await contextA.SaveChangesAsync();

        // contextB conserva lock_version = 1 en su snapshot original -- su
        // propio UPDATE seguirá generando "WHERE ... AND lock_version = 1",
        // que ya NO coincide con la fila física (ahora en 2) tras el
        // SaveChanges de A. Cero filas afectadas => EF interpreta esto como
        // un conflicto de concurrencia.
        contextB.Entry(criterionInB).Property(nameof(criterionInB.AuditedResponse)).CurrentValue = "Respuesta de B (obsoleta)";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync());
    }
}
