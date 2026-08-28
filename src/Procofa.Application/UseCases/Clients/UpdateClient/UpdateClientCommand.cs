namespace Procofa.Application.UseCases.Clients.UpdateClient;

/// <summary><c>PUT /api/clients/{clientId}</c>. <see cref="ProgramCodes"/> nulo significa "no
/// tocar los programas asignados"; un array (incluso vacío) reemplaza el conjunto completo.</summary>
public sealed record UpdateClientCommand(
    Guid ClientId,
    string? LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    string? Notes,
    IReadOnlyCollection<string>? ProgramCodes);
