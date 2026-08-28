using Procofa.Application.Abstractions.Identity;
using Procofa.Domain.Entities.Identity;

namespace Procofa.Infrastructure.Persistence.Repositories;

/// <summary>Implementación de <see cref="IAccessLogRepository"/> sobre <see
/// cref="ProcofaDbContext"/>.</summary>
public sealed class AccessLogRepository(ProcofaDbContext dbContext) : IAccessLogRepository
{
    public Task AddAsync(AccessLog accessLog, CancellationToken cancellationToken)
    {
        dbContext.AccessLogs.Add(accessLog);
        return Task.CompletedTask;
    }
}
