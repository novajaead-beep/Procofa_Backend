namespace Procofa.Domain.Entities.Audits.ValueObjects;

/// <summary>
/// Programa (OEA/C-TPAT) bajo el cual se ejecuta una <see cref="Audit"/>.
/// Tabla física: <c>audit_programs</c> — PK compuesta
/// <c>(audit_id, program_id)</c>, tenant-scoped, sin columna <c>id</c>, sin
/// atributos propios. Colección owned dentro de <see cref="Audit.Programs"/>,
/// sin <c>DbSet</c> propio.
/// </summary>
public sealed class AuditProgram
{
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public Guid ProgramId { get; private set; }

    private AuditProgram() { }

    public AuditProgram(Guid tenantId, Guid auditId, Guid programId)
    {
        TenantId = tenantId;
        AuditId = auditId;
        ProgramId = programId;
    }
}
