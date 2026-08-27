using Procofa.Application.Abstractions.Identity;
using Procofa.Application.Abstractions.Tenancy;
using Procofa.Application.UseCases.Users;

namespace Procofa.Application.UseCases.Users.ListUsers;

/// <summary>
/// Caso de uso <c>GET /api/users</c> (Instrucción 05). Solo lectura — corre
/// dentro de <see cref="ITenantUnitOfWork.ExecuteReadAsync{T}"/> (BEGIN READ
/// ONLY + SET LOCAL app.tenant_id), nunca fuera de una transacción
/// tenant-scoped.
/// </summary>
public sealed class ListUsersQueryHandler(
    ITenantContext tenantContext,
    ITenantUnitOfWork unitOfWork,
    IUserRepository userRepository)
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    public Task<ListUsersResult> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        if (query.Role is not null && !UserRoleCodes.All.Contains(query.Role))
        {
            return Task.FromResult(ListUsersResult.Failure(ListUsersError.InvalidRole));
        }

        var tenantId = tenantContext.TenantId;
        var page = query.Page < 1 ? DefaultPage : query.Page;
        var pageSize = query.PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize,
        };

        return unitOfWork.ExecuteReadAsync(
            async ct =>
            {
                var pageResult = await userRepository.ListAsync(
                    tenantId, query.Search, query.IsActive, query.Role, page, pageSize, ct);

                return ListUsersResult.Success(pageResult.Items, page, pageSize, pageResult.Total);
            },
            cancellationToken);
    }
}
