namespace Procofa.Api.Contracts.Clients;

public sealed record CreateClientRequest(
    string? LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    string? Notes,
    IReadOnlyCollection<string>? Programs);

public sealed record CreateClientResponse(Guid Id);
