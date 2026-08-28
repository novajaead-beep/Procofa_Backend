using Procofa.Domain.Entities.Identity;

namespace Procofa.Application.Abstractions.Identity;

/// <summary>
/// Puerto de persistencia de <see cref="AccessLog"/>. Actualmente solo escribe
/// <c>LOGIN_SUCCESS</c>/<c>LOGIN_FAILURE</c> — el CHECK físico permite más valores, pero ampliarlos
/// queda para una futura extensión (logout, password reset). </summary>
public interface IAccessLogRepository
{
    Task AddAsync(AccessLog accessLog, CancellationToken cancellationToken);
}
