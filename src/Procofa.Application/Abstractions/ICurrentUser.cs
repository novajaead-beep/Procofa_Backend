namespace Procofa.Application.Abstractions;

/// <summary>
/// abstracción mínima para que los casos de uso que necesitan impedir auto-desactivación o
/// auto-remoción de rol ADMIN obtengan el usuario autenticado SIN depender de <c>HttpContext</c>
/// (prohibido en Application). El <c>userId</c> viene siempre de la claim <c>sub</c> del JWT ya
/// validado — nunca del body del request. La implementación HTTP real vive en Api (ver
/// <c>Procofa.Api.Security.HttpContextCurrentUser</c>). </summary>
public interface ICurrentUser
{
    Guid UserId { get; }

    /// <summary>Códigos de rol (claim <c>roles</c> del JWT ya validado) del usuario autenticado —
    /// usado para el alcance de lectura de CLIENTE en Clients/Companies/Sites/Contacts (nunca para
    /// decisiones de escritura, que siguen resueltas por <c>[Authorize(Roles=...)]</c> a nivel de
    /// endpoint).</summary>
    IReadOnlyCollection<string> Roles { get; }
}
