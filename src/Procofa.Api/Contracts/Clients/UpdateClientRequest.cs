namespace Procofa.Api.Contracts.Clients;

/// <summary><see cref="Programs"/> nulo = no tocar los programas asignados; un array (incluso
/// vacío) reemplaza el conjunto completo.</summary>
public sealed record UpdateClientRequest(
    string? LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    string? Notes,
    IReadOnlyCollection<string>? Programs);
