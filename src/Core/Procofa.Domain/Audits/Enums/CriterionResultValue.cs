namespace Procofa.Domain.Audits.Enums;

/// <summary>Resultado registrado por el auditor sobre un criterio del checklist (HU-08).</summary>
public enum CriterionResultValue
{
    NotAnswered = 0,
    Compliant = 1,
    NonCompliant = 2,
    NotApplicable = 3
}
