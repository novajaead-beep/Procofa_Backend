namespace Procofa.Application.UseCases.Companies.GetCompany;

public enum GetCompanyError
{
    NotFound,
}

public sealed class GetCompanyResult
{
    public bool IsSuccess { get; }
    public GetCompanyError? Error { get; }
    public Guid Id { get; }
    public Guid ClientId { get; }
    public Guid? DefaultProfileId { get; }
    public string LegalName { get; } = string.Empty;
    public string? TradeName { get; }
    public string? TaxId { get; }
    public string? Industry { get; }
    public string? CompanyType { get; }
    public bool IsClientCompany { get; }
    public bool IsActive { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime UpdatedAtUtc { get; }

    private GetCompanyResult(bool isSuccess, GetCompanyError? error) { IsSuccess = isSuccess; Error = error; }

    private GetCompanyResult(
        Guid id, Guid clientId, Guid? defaultProfileId, string legalName, string? tradeName, string? taxId,
        string? industry, string? companyType, bool isClientCompany, bool isActive, DateTime createdAtUtc,
        DateTime updatedAtUtc)
        : this(true, null)
    {
        Id = id;
        ClientId = clientId;
        DefaultProfileId = defaultProfileId;
        LegalName = legalName;
        TradeName = tradeName;
        TaxId = taxId;
        Industry = industry;
        CompanyType = companyType;
        IsClientCompany = isClientCompany;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static GetCompanyResult Success(
        Guid id, Guid clientId, Guid? defaultProfileId, string legalName, string? tradeName, string? taxId,
        string? industry, string? companyType, bool isClientCompany, bool isActive, DateTime createdAtUtc,
        DateTime updatedAtUtc) =>
        new(id, clientId, defaultProfileId, legalName, tradeName, taxId, industry, companyType, isClientCompany,
            isActive, createdAtUtc, updatedAtUtc);

    public static GetCompanyResult NotFound() => new(false, GetCompanyError.NotFound);
}
