using Microsoft.EntityFrameworkCore;
using Procofa.Application.Abstractions.Catalogs;
using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Infrastructure.Persistence.Repositories;

/// <summary>Implementación de <see cref="IProgramRepository"/>. Catálogo global sin
/// <c>tenant_id</c>, sin RLS — no hay filtro de tenant que aplicar aquí.</summary>
public sealed class ProgramRepository(ProcofaDbContext dbContext) : IProgramRepository
{
    public async Task<IReadOnlyCollection<ComplianceProgram>> FindManyByCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken cancellationToken)
    {
        if (codes.Count == 0)
        {
            return [];
        }

        return await dbContext.CompliancePrograms.Where(p => codes.Contains(p.Code)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetCodesByIdsAsync(
        IReadOnlyCollection<Guid> programIds, CancellationToken cancellationToken)
    {
        if (programIds.Count == 0)
        {
            return [];
        }

        return await dbContext.CompliancePrograms
            .Where(p => programIds.Contains(p.Id))
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);
    }
}
