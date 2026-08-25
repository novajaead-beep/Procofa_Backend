using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="ObservationType"/> ↔
/// <c>observations.observation_type varchar(30)</c> (Instrucción 03.1, defecto 1).
/// </summary>
public sealed class ObservationTypeConverter : ValueConverter<ObservationType, string>
{
    public ObservationTypeConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string ToDb(ObservationType value) => value switch
    {
        ObservationType.Auditor => "AUDITOR",
        ObservationType.Cliente => "CLIENTE",
        ObservationType.Interna => "INTERNA",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(ObservationType)} sin mapeo físico explícito."),
    };

    private static ObservationType FromDb(string value) => value switch
    {
        "AUDITOR" => ObservationType.Auditor,
        "CLIENTE" => ObservationType.Cliente,
        "INTERNA" => ObservationType.Interna,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(ObservationType)}."),
    };
}
