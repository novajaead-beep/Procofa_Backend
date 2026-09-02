namespace Procofa.Application.UseCases.Audits.ReplaceAuditPrograms;

/// <summary><c>PUT /api/audits/{auditId}/programs</c>. <see cref="ProgramCodes"/> llega tal como el
/// cliente lo declaró (código de programa, ej. "OEA"/"CTPAT"), nunca un GUID — mismo contrato que
/// <c>CreateAuditCommand.ProgramCodes</c> y <c>CreateClientCommand.ProgramCodes</c>; se resuelve
/// contra el catálogo por código.</summary>
public sealed record ReplaceAuditProgramsCommand(Guid AuditId, IReadOnlyCollection<string>? ProgramCodes);
