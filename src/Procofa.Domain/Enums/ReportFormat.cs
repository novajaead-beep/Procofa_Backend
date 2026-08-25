namespace Procofa.Domain.Enums;

/// <summary>
/// Formato de archivo de un <c>AuditReport</c> generado.
/// Respaldado por <c>audit_reports.format varchar(10)</c> con
/// <c>CONSTRAINT audit_reports_format_check CHECK (format IN
/// ('PDF','DOCX','XLSX'))</c>.
/// </summary>
public enum ReportFormat
{
    Pdf,
    Docx,
    Xlsx
}
