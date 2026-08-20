namespace Procofa.Domain.Audits.Enums;

/// <summary>Perfil normativo bajo el cual se ejecuta la auditoría. Determina el checklist maestro a cargar (HU-03).</summary>
public enum AuditProfileType
{
    Oea = 1,
    CTpat = 2,
    Both = 3
}
