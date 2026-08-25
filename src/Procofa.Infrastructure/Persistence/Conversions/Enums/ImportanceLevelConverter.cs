using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Procofa.Domain.Enums;

namespace Procofa.Infrastructure.Persistence.Conversions.Enums;

/// <summary>
/// Contrato explícito <see cref="ImportanceLevel"/> ↔
/// <c>criteria.importance_level varchar(20)</c> (nullable) — Instrucción
/// 03.1, defecto 1. Única propiedad nullable entre los 16 enums VARCHAR+CHECK
/// del baseline (ver <c>Criterion.ImportanceLevel</c>: <c>ImportanceLevel?</c>),
/// de ahí el <see cref="ValueConverter{TModel,TProvider}"/> sobre los tipos
/// nullable en ambos lados en vez de <see cref="ImportanceLevel"/>/<see cref="string"/> puros.
/// </summary>
public sealed class ImportanceLevelConverter : ValueConverter<ImportanceLevel?, string?>
{
    public ImportanceLevelConverter() : base(v => ToDb(v), v => FromDb(v)) { }

    private static string? ToDb(ImportanceLevel? value) => value switch
    {
        null => null,
        ImportanceLevel.Alta => "ALTA",
        ImportanceLevel.Media => "MEDIA",
        ImportanceLevel.Baja => "BAJA",
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"{nameof(ImportanceLevel)} sin mapeo físico explícito."),
    };

    private static ImportanceLevel? FromDb(string? value) => value switch
    {
        null => null,
        "ALTA" => ImportanceLevel.Alta,
        "MEDIA" => ImportanceLevel.Media,
        "BAJA" => ImportanceLevel.Baja,
        _ => throw new ArgumentOutOfRangeException(
            nameof(value), value, $"Valor físico sin mapeo a {nameof(ImportanceLevel)}."),
    };
}
