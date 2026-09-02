namespace Procofa.Application.UseCases.Audits.CreateAudit;

/// <summary><c>POST /api/audits</c>. <see cref="ProgramCodes"/> llega tal como el cliente lo
/// envió — código de programa, nunca un GUID (mismo contrato que <c>CreateClientCommand
/// .ProgramCodes</c>); TODA validación (catálogo, pertenencia, existencia) ocurre dentro del
/// handler. <see cref="ExecutionMode"/> es el string físico ("ONSITE"/"REMOTE"/"HYBRID") tal como
/// lo envía el request.</summary>
public sealed record CreateAuditCommand(
    Guid? ClientId,
    Guid? AuditedCompanyId,
    Guid? CompanySiteId,
    Guid? AuditTypeId,
    Guid? ProfileId,
    IReadOnlyCollection<string>? ProgramCodes,
    string? Objective,
    string? Scope,
    string? Methodology,
    DateOnly? ScheduledDate,
    string? ExecutionMode);
