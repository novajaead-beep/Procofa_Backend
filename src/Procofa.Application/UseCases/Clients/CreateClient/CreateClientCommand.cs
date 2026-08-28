namespace Procofa.Application.UseCases.Clients.CreateClient;

/// <summary><c>POST /api/clients</c>. <see cref="ProgramCodes"/> llega tal como el cliente lo
/// envió — TODA validación (catálogo cerrado OEA/CTPAT, existencia) ocurre dentro del handler.
/// </summary>
public sealed record CreateClientCommand(
    string? LegalName,
    string? TradeName,
    string? TaxId,
    string? Industry,
    string? CompanyType,
    string? Notes,
    IReadOnlyCollection<string>? ProgramCodes);
