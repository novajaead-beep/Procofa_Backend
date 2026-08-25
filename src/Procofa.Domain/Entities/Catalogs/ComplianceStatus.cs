namespace Procofa.Domain.Entities.Catalogs;

/// <summary>
/// Resultado de cumplimiento de un <c>AuditCriterion</c> (CUMPLE=100,
/// CUMPLE_PARCIAL=50, NO_CUMPLE=0, NO_APLICA=excluido del score). Catálogo
/// global sin <c>tenant_id</c>, sin RLS, solo lectura para
/// <c>procofa_app</c>. Tabla física: <c>compliance_statuses</c>, 4 filas
/// sembradas.
/// </summary>
public sealed class ComplianceStatus
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    /// <summary><c>numeric(5,2)</c>, nullable — ver <c>ComplianceStatusConfiguration</c>.</summary>
    public decimal? ScoreWeight { get; private set; }

    public bool IncludedInScore { get; private set; } = true;
    public int SortOrder { get; private set; }

    private ComplianceStatus() { }

    public ComplianceStatus(Guid id, string code, string name, decimal? scoreWeight, bool includedInScore, int sortOrder)
    {
        Id = id;
        Code = code;
        Name = name;
        ScoreWeight = scoreWeight;
        IncludedInScore = includedInScore;
        SortOrder = sortOrder;
    }
}
