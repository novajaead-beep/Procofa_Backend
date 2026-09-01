using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Repositories;

public sealed class ProfileRepository(ProcofaDbContext dbContext) : IProfileRepository
{
    public Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Profile?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
}
