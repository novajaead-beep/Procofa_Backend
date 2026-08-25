using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="ExecutionMode"/> ↔
/// <c>audits.execution_mode varchar(20)</c> (constraint
/// <c>ck_audits_execution_mode</c>) — Instrucción 03.1, defecto 1.
/// </summary>
public sealed class ExecutionModeConverter : ValueConverter<ExecutionMode, string>
{
    public ExecutionModeConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(ExecutionMode value) => value switch
    {
        ExecutionMode.Onsite => "ONSITE",
        ExecutionMode.Remote => "REMOTE",
        ExecutionMode.Hybrid => "HYBRID",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(ExecutionMode)} sin mapeo físico explícito."),
    };

    private static ExecutionMode FromDb(string value) => value switch
    {
        "ONSITE" => ExecutionMode.Onsite,
        "REMOTE" => ExecutionMode.Remote,
        "HYBRID" => ExecutionMode.Hybrid,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(ExecutionMode)}."),
    };
}
