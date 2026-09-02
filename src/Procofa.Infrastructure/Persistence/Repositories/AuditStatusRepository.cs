using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class AuditStatusRepository(ProcofaDbContext dbContext) : IAuditStatusRepository
{
    public Task<AuditStatus?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.AuditStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Code == code, cancellationToken);

    public Task<AuditStatus?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AuditStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
}
