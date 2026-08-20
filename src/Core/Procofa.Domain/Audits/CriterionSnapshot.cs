using Procofa.Domain.Common;

namespace Procofa.Domain.Audits;

/// <summary>
/// Copia inmutable de un criterio del checklist maestro, generada una única vez al cargar el plan
/// (HU-03). No expone métodos de mutación: una vez creada, ningún campo cambia — garantiza
/// trazabilidad histórica aun si el checklist maestro se actualiza posteriormente (HU-04).
/// </summary>
public sealed class CriterionSnapshot
{
    public Guid Id { get; private set; }
    public Guid AuditPlanId { get; private set; }
    public Guid SourceMasterCriterionId { get; private set; }
    public int SourceChecklistVersion { get; private set; }
    public string Section { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public bool IsMandatory { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private CriterionSnapshot() { } // EF Core

    private CriterionSnapshot(
        Guid id, Guid auditPlanId, Guid sourceMasterCriterionId, int sourceChecklistVersion,
        string section, string code, string description, bool isMandatory, int displayOrder)
    {
        Id = id;
        AuditPlanId = auditPlanId;
        SourceMasterCriterionId = sourceMasterCriterionId;
        SourceChecklistVersion = sourceChecklistVersion;
        Section = section;
        Code = code;
        Description = description;
        IsMandatory = isMandatory;
        DisplayOrder = displayOrder;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Fábrica de snapshot a partir de un criterio del checklist maestro (HU-03/HU-04).
    /// Los datos del maestro se reciben como escalares para no acoplar el Domain de Ejecución
    /// a la entidad del módulo de Planificación/Checklist Maestro (bounded context distinto).
    /// </summary>
    public static CriterionSnapshot FromMaster(
        Guid auditPlanId,
        Guid masterCriterionId,
        int checklistVersion,
        string section,
        string code,
        string description,
        bool isMandatory,
        int displayOrder)
    {
        if (auditPlanId == Guid.Empty)
            throw new DomainException("El snapshot de criterio requiere un plan de auditoría válido.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("El criterio requiere un código.");
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("El criterio requiere una descripción.");

        return new CriterionSnapshot(
            Guid.NewGuid(), auditPlanId, masterCriterionId, checklistVersion,
            section, code, description, isMandatory, displayOrder);
    }
}
