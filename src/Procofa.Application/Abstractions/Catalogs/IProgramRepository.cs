using Procofa.Domain.Entities.Catalogs;

namespace Procofa.Application.Abstractions.Catalogs;

/// <summary>Puerto de solo-lectura sobre el catálogo <see cref="ComplianceProgram"/> (tabla física
/// <c>programs</c> — OEA/C-TPAT). Identidad semántica estable por <see
/// cref="ComplianceProgram.Code"/>, nunca por UUID hardcodeado (decisión congelada #5, baseline
/// V2.1).</summary>
public interface IProgramRepository
{
    /// <summary>Resuelve varios programas de una vez por <see cref="ComplianceProgram.Code"/> —
    /// códigos que no existen en el catálogo simplemente no aparecen en el resultado; el caller
    /// compara la cuenta para detectar códigos inválidos.</summary>
    Task<IReadOnlyCollection<ComplianceProgram>> FindManyByCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken cancellationToken);

    /// <summary>Resuelve los <see cref="ComplianceProgram.Code"/> de un conjunto de <c>program_id</c>
    /// — usado para proyectar <c>Client.Programs</c> (que solo guarda el id) a códigos legibles.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetCodesByIdsAsync(
        IReadOnlyCollection<Guid> programIds, CancellationToken cancellationToken);

    Task<ComplianceProgram?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ComplianceProgram?> FindByCodeAsync(string code, CancellationToken cancellationToken);
}
