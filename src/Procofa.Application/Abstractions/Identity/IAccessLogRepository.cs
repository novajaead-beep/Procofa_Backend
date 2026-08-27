using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// Puerto de persistencia de <see cref="AccessLog"/> (Instrucción 04). Esta
/// instrucción solo escribe <c>LOGIN_SUCCESS</c>/<c>LOGIN_FAILURE</c> — el
/// CHECK físico permite más valores, pero ampliarlos es alcance de
/// instrucciones futuras (logout, password reset).
/// </summary>
public interface IAccessLogRepository
{
    Task AddAsync(AccessLog accessLog, CancellationToken cancellationToken);
}
