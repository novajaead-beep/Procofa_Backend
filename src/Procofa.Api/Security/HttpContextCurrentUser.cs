using System.IdentityModel.Tokens.Jwt;
using Procofa.Application.Abstractions;

namespace Procofa.Api.Security;

/// <summary>
/// Implementación HTTP de <see cref="ICurrentUser"/> (Instrucción 05, sección
/// "IDENTIDAD DEL ADMIN ACTUAL"). Único lugar de todo el proceso que lee
/// <c>HttpContext</c> para resolver el usuario autenticado — Application
/// nunca lo toca directamente. El id sale SIEMPRE de la claim <c>sub</c> del
/// JWT ya validado por el middleware de autenticación (nunca del body del
/// request, nunca de un header custom).
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var httpContext = httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException(
                    "ICurrentUser se resolvió fuera de un HttpContext activo (¿se está usando desde " +
                    "un caso de uso que también corre en modo host, como bootstrap-admin?).");

            var subClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new InvalidOperationException(
                    "El claim 'sub' no está presente en el usuario autenticado — no debería ser posible " +
                    "llegar aquí en un endpoint con [Authorize].");

            if (!Guid.TryParse(subClaim, out var userId))
            {
                throw new InvalidOperationException("El claim 'sub' del JWT no es un GUID válido.");
            }

            return userId;
        }
    }
}
