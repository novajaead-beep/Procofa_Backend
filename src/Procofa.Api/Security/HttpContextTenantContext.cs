using Procofa.Application.Abstractions.Tenancy;
using Procofa.Infrastructure;

namespace Procofa.Api.Security;

public sealed class HttpContextTenantContext(
    IHttpContextAccessor httpContextAccessor,
    InfrastructureAuthSettings settings)
    : ITenantContext
{
    public Guid TenantId
    {
        get
        {
            var httpContext =
                httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException(
                    "ITenantContext se resolvió fuera de un HttpContext.");

            if (httpContext.User.Identity?.IsAuthenticated
                != true)
            {
                return settings.ProcofaTenantId;
            }

            var tenantClaim =
                httpContext.User.FindFirst("tenant_id")?.Value
                ?? throw new InvalidOperationException(
                    "El JWT autenticado no contiene tenant_id.");

            if (!Guid.TryParse(
                    tenantClaim,
                    out var tenantId) ||
                tenantId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "El claim tenant_id del JWT no es un GUID válido.");
            }

            // Stage 1 es single-tenant: el único tenant válido es el fijo de configuración. Un
            // tenant_id bien formado pero distinto no se acepta — evita que un JWT firmado con un
            // tenant_id arbitrario (aunque válido como GUID) sea tratado como el tenant operativo.
            if (tenantId != settings.ProcofaTenantId)
            {
                throw new InvalidOperationException(
                    "El claim tenant_id del JWT no corresponde al tenant configurado.");
            }

            return tenantId;
        }
    }
}
