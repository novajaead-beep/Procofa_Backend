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
}
