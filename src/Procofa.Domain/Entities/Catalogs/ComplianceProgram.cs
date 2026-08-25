namespace Procofa.Domain.Entities.Catalogs;

/// <summary>
/// Programa de cumplimiento (OEA, C-TPAT). Catálogo global sin
/// <c>tenant_id</c>, sin RLS. Tabla física: <c>programs</c>, 2 filas
/// sembradas. Identidad semántica estable: <see cref="Code"/> — nunca
/// hardcodear el UUID (decisión congelada #5). <c>procofa_app</c> hoy solo
/// tiene GRANT SELECT; será DML-administrable por ADMIN a futuro vía una
/// migración de GRANTs controlada, todavía no aplicada (baseline V2.1,
/// decisión congelada #9).
///
/// Nombrada <c>ComplianceProgram</c> en vez de <c>Program</c> (que es el
/// nombre físico de la tabla) para no colisionar con la clase implícita
/// <c>Program</c> que generan los top-level statements de
/// <c>Procofa.Api/Program.cs</c> — el mapeo de tabla explícito
/// (<c>ToTable("programs")</c>) vive en <c>ComplianceProgramConfiguration</c>.
/// </summary>
public sealed class ComplianceProgram
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ComplianceProgram() { }

    public ComplianceProgram(Guid id, string code, string name, string? description)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
        IsActive = true;
    }
}
