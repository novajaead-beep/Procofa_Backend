using Procofa.Application.Abstractions.Tenancy;

namespace Procofa.Infrastructure.Persistence.Tenancy;

/// <summary>
/// Implementación Stage 1 de <see cref="ITenantContext"/>: el tenant efectivo NUNCA viene del
/// request — se resuelve siempre desde configuración, fijo, al único tenant PROCOFA. Registrada con
/// lifetime <c>Scoped</c> (no <c>Singleton</c>) a propósito, aunque en Stage 1 el valor jamás
/// varíe: la instrucción futura de requests autenticados resolverá el tenant desde el claim
/// <c>tenant_id</c> del JWT (por request), y esa implementación reemplazará esta clase manteniendo
/// el mismo lifetime — evita un cambio de lifetime a mitad de la vida del proyecto en toda la
/// cadena de consumidores. </summary>
public sealed class Stage1TenantContext : ITenantContext
{
    public Stage1TenantContext(Guid procofaTenantId)
    {
        if (procofaTenantId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "El tenant PROCOFA Stage 1 no está configurado (Guid.Empty) — " +
                "verifique InfrastructureAuthSettings.ProcofaTenantId.");
        }

        TenantId = procofaTenantId;
    }

    public Guid TenantId { get; }
}
