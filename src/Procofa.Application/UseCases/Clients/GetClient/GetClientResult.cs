namespace Procofa.Application.UseCases.Clients.GetClient;

public enum GetClientError
{
    /// <summary>No existe, pertenece a otro tenant, o no es visible bajo el alcance de CLIENTE —
    /// respuesta idéntica (404) en los tres casos, nunca 403.</summary>
    NotFound,
}

public sealed class GetClientResult
{
    public bool IsSuccess { get; }
    public GetClientError? Error { get; }
    public Guid Id { get; }
    public string LegalName { get; } = string.Empty;
    public string? TradeName { get; }
    public string? TaxId { get; }
    public string? Industry { get; }
    public string? CompanyType { get; }
    public string? Notes { get; }
    public bool IsActive { get; }
    public IReadOnlyCollection<string> Programs { get; } = [];
    public int AuditedCompanyCount { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; }

    private GetClientResult(bool isSuccess, GetClientError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    private GetClientResult(
        Guid id, string legalName, string? tradeName, string? taxId, string? industry, string? companyType,
        string? notes, bool isActive, IReadOnlyCollection<string> programs, int auditedCompanyCount,
        DateTime createdAtUtc, DateTime updatedAtUtc)
        : this(true, null)
    {
        Id = id;
        LegalName = legalName;
        TradeName = tradeName;
        TaxId = taxId;
        Industry = industry;
        CompanyType = companyType;
        Notes = notes;
        IsActive = isActive;
        Programs = programs;
        AuditedCompanyCount = auditedCompanyCount;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GetClientResult Success(
        Guid id, string legalName, string? tradeName, string? taxId, string? industry, string? companyType,
        string? notes, bool isActive, IReadOnlyCollection<string> programs, int auditedCompanyCount,
        DateTime createdAtUtc, DateTime updatedAtUtc) =>
        new(id, legalName, tradeName, taxId, industry, companyType, notes, isActive, programs,
            auditedCompanyCount, createdAtUtc, updatedAtUtc);

    public static GetClientResult NotFound() => new(false, GetClientError.NotFound);
}
