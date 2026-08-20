using Microsoft.EntityFrameworkCore;
using Procofa.Application.Ports.Out;
using Procofa.Domain.Audits;

namespace Procofa.Adapters.Persistence.PostgreSQL.Repositories;

public sealed class AuditRepository : IAuditRepository
{
    private readonly ProcofaDbContext _db;

    public AuditRepository(ProcofaDbContext db) => _db = db;

    public Task<AuditPlan?> GetPlanByIdAsync(Guid auditPlanId, CancellationToken cancellationToken)
        => _db.AuditPlans.FirstOrDefaultAsync(p => p.Id == auditPlanId, cancellationToken);

    public Task<AuditPlan?> GetPlanWithChecklistAsync(Guid auditPlanId, CancellationToken cancellationToken)
        => _db.AuditPlans.Include(p => p.Checklist).FirstOrDefaultAsync(p => p.Id == auditPlanId, cancellationToken);

    public async Task<IReadOnlyCollection<AuditPlan>> GetPlansByClientAsync(Guid clientId, CancellationToken cancellationToken)
        => await _db.AuditPlans.Where(p => p.ClientId == clientId).ToListAsync(cancellationToken);

    public async Task AddPlanAsync(AuditPlan auditPlan, CancellationToken cancellationToken)
        => await _db.AuditPlans.AddAsync(auditPlan, cancellationToken);

    public Task<AuditResult?> GetResultByCriterionAsync(Guid criterionSnapshotId, CancellationToken cancellationToken)
        => _db.AuditResults.FirstOrDefaultAsync(r => r.CriterionSnapshotId == criterionSnapshotId, cancellationToken);

    public async Task<IReadOnlyCollection<AuditResult>> GetResultsByPlanAsync(Guid auditPlanId, CancellationToken cancellationToken)
        => await _db.AuditResults.Where(r => r.AuditPlanId == auditPlanId).ToListAsync(cancellationToken);

    public async Task SaveResultAsync(AuditResult result, CancellationToken cancellationToken)
    {
       
        _db.Update(result);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Finding?> GetFindingByIdAsync(Guid findingId, CancellationToken cancellationToken)
        => _db.Findings.FirstOrDefaultAsync(f => f.Id == findingId, cancellationToken);

    public async Task<IReadOnlyCollection<Finding>> GetFindingsByPlanAsync(Guid auditPlanId, CancellationToken cancellationToken)
        => await _db.Findings.Where(f => f.AuditPlanId == auditPlanId).ToListAsync(cancellationToken);

    public async Task AddFindingAsync(Finding finding, CancellationToken cancellationToken)
        => await _db.Findings.AddAsync(finding, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);
}
