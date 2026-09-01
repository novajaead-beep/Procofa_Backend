using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class AuditTypeRepository(ProcofaDbContext dbContext) : IAuditTypeRepository
{
    public Task<AuditType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.AuditTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<AuditType?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.AuditTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Code == code, cancellationToken);
}
