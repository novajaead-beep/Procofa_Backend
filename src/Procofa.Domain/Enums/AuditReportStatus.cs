namespace Procofa.Domain.Enums;

/// <summary>
/// Estado de una versión de <c>AuditReport</c>.
/// Respaldado por <c>audit_reports.status varchar(20) DEFAULT 'DRAFT'</c> con
/// <c>CONSTRAINT audit_reports_status_check CHECK (status IN
/// ('DRAFT','FINAL','VOID'))</c>.
///
/// Un reporte en <see cref="Final"/> es inmutable a nivel de BD: el trigger
/// <c>trg_audit_reports_final_immutable</c> (función
/// <c>prevent_final_report_mutation()</c>) rechaza UPDATE y DELETE cuando
/// <c>OLD.status = 'FINAL'</c>.
/// </summary>
public enum AuditReportStatus
{
    Draft,
    Final,
    Void
}
