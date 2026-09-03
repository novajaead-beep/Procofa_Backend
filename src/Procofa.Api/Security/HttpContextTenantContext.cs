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

            return tenantId;
        }
    }
}
