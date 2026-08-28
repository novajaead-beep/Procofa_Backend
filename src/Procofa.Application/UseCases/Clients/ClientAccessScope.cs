using Procofa.Application.Abstractions;
using Procofa.Application.Abstractions.Identity;
using Procofa.Application.UseCases.Users;

namespace Procofa.Application.UseCases.Clients;

/// <summary>
/// Resuelve el alcance de lectura de Clients/Companies/Sites/Contacts según el rol del usuario
/// autenticado: ADMIN/AUDITOR_LIDER/AUDITOR_APOYO/CONSULTOR leen todo el tenant; CLIENTE solo los
/// clients asignados vía <c>user_client_access</c>. No crea un
/// sistema nuevo de permisos — solo lee <see cref="ICurrentUser.Roles"/> (claims del JWT ya
/// validado) y, para CLIENTE, la colección <c>User.ClientAccess</c> ya persistida.
/// </summary>
public static class ClientAccessScope
{
    private static readonly string[] FullReadRoles =
    [
        UserRoleCodes.Admin,
        UserRoleCodes.AuditorLider,
        UserRoleCodes.AuditorApoyo,
        UserRoleCodes.Consultor,
    ];

    /// <summary><c>null</c> = sin restricción (lee todo el tenant). Un conjunto no-nulo (posiblemente
    /// vacío) acota la lectura a esos <c>clientId</c> — nunca se interpreta un conjunto vacío como
    /// "sin restricción".</summary>
    public static async Task<IReadOnlyCollection<Guid>?> ResolveAsync(
        ICurrentUser currentUser, Guid tenantId, IUserRepository userRepository, CancellationToken cancellationToken)
    {
        if (currentUser.Roles.Any(FullReadRoles.Contains))
        {
            return null;
        }

        if (!currentUser.Roles.Contains(UserRoleCodes.Cliente))
        {
            // Sin un rol reconocido con permiso de lectura: alcance vacío por defecto
            // (fail-closed), nunca acceso implícito a todo el tenant.
            return [];
        }

        var user = await userRepository.GetByIdAsync(tenantId, currentUser.UserId, cancellationToken);
        return user?.ClientAccess.Select(a => a.ClientId).ToArray() ?? [];
    }

    /// <summary><c>true</c> si el cliente es visible bajo el alcance resuelto por <see
    /// cref="ResolveAsync"/> — usado por Get* para decidir si el recurso existente pero fuera de
    /// alcance responde 404 en vez de 403.</summary>
    public static bool IsVisible(IReadOnlyCollection<Guid>? scope, Guid clientId) =>
        scope is null || scope.Contains(clientId);
}
