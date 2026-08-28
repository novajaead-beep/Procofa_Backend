namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// JWT de acceso ya emitido, listo para devolver al cliente.
/// </summary>
/// <param name="Value">Token JWT compacto (header.payload.signature).</param>
/// <param name="ExpiresAtUtc">Instante UTC de expiración — mismo valor que el claim <c>exp</c>, en forma consultable sin decodificar el token.</param>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

/// <summary>
/// Puerto de emisión de JWT de acceso. Claims mínimos que la implementación debe incluir:
/// <c>sub</c> (user id), <c>tenant_id</c>, <c>email</c>, <c>roles</c> (uno o más claims con los
/// códigos exactos de <see cref="Procofa.Domain.Entities.Identity.Role.Code"/> del usuario — nunca
/// aceptados desde el request, siempre resueltos server-side). Issuer, audience, signing key y
/// minutos de expiración son configuración de Infrastructure (appsettings/environment), no de este
/// puerto. </summary>
public interface IAccessTokenGenerator
{
    AccessToken GenerateAccessToken(
        Guid userId,
        Guid tenantId,
        string email,
        IReadOnlyCollection<string> roleCodes);
}
