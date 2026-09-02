using Procofa.Domain.Enums;

namespace Procofa.Application.UseCases.Audits;

/// <summary>Traduce el string físico de <c>audits.execution_mode</c> ("ONSITE"/"REMOTE"/"HYBRID",
/// tal como lo envía el request) al enum de dominio — compartido por <c>CreateAudit</c> y
/// <c>UpdateAudit</c> para no duplicar el switch.</summary>
public static class ExecutionModeParser
{
    public static bool TryParse(string? value, out ExecutionMode executionMode)
    {
        switch (value)
        {
            case "ONSITE":
                executionMode = ExecutionMode.Onsite;
                return true;
            case "REMOTE":
                executionMode = ExecutionMode.Remote;
                return true;
            case "HYBRID":
                executionMode = ExecutionMode.Hybrid;
                return true;
            default:
                executionMode = default;
                return false;
        }
    }

    public static string ToRequestString(ExecutionMode executionMode) => executionMode switch
    {
        ExecutionMode.Onsite => "ONSITE",
        ExecutionMode.Remote => "REMOTE",
        ExecutionMode.Hybrid => "HYBRID",
        _ => throw new ArgumentOutOfRangeException(nameof(executionMode), executionMode, null),
    };

    /// <summary>ONSITE/HYBRID exigen <c>company_site_id</c>; REMOTE lo deja opcional — mismo
    /// invariante reflejado en <c>Audit.EnsureExecutionModeMatchesSite</c>, evaluado aquí ANTES de
    /// tocar el aggregate para que el handler devuelva un <c>Result</c> tipado en vez de dejar que
    /// el constructor/<c>UpdateDetails</c> lance.</summary>
    public static bool RequiresCompanySite(ExecutionMode executionMode) =>
        executionMode is ExecutionMode.Onsite or ExecutionMode.Hybrid;
}
