namespace Procofa.Domain.Enums;

/// <summary>
/// Ciclo de vida de una <c>ChecklistVersion</c>.
/// Respaldado por <c>checklist_versions.status varchar(20) DEFAULT 'DRAFT'</c>
/// con <c>CHECK (status IN ('DRAFT','PUBLISHED','RETIRED'))</c>.
///
/// Deliberadamente NO se comparte el tipo C# con
/// <see cref="ReportTemplateVersionStatus"/> aunque ambos usan hoy las mismas
/// 3 cadenas: son dos máquinas de estado independientes (checklist vs
/// plantilla de reporte) que sólo coinciden por vocabulario, no por ser el
/// mismo concepto de dominio — mantenerlos separados evita acoplar su
/// evolución futura (ej. si una de las dos agrega un estado nuevo).
///
/// La inmutabilidad de una versión <see cref="Published"/> (secciones/
/// criterios ya no editables) se enforza hoy en Application, no con un
/// trigger SQL análogo a <c>prevent_final_report_mutation()</c> (baseline
/// V2.1, hallazgo 🟢 sección C — defensa SQL simétrica queda pendiente,
/// no bloquea Foundation).
/// </summary>
public enum ChecklistVersionStatus
{
    Draft,
    Published,
    Retired
}
