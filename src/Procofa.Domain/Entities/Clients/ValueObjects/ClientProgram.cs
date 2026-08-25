namespace Procofa.Domain.Entities.Clients.ValueObjects;

/// <summary>
/// Programa (OEA/C-TPAT) al que está inscrito un <see cref="Client"/>.
/// Tabla física: <c>client_programs</c> — PK compuesta
/// <c>(client_id, program_id)</c>, tenant-scoped, sin columna <c>id</c>,
/// sin atributos propios. Colección owned dentro de
/// <see cref="Client.Programs"/>, sin <c>DbSet</c> propio.
/// </summary>
public sealed class ClientProgram
{
    public Guid TenantId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid ProgramId { get; private set; }

    private ClientProgram() { }

    public ClientProgram(Guid tenantId, Guid clientId, Guid programId)
    {
        TenantId = tenantId;
        ClientId = clientId;
        ProgramId = programId;
    }
}
